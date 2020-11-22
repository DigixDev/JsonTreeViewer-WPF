using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

/*  Code by: Mehran Sivari  */
namespace App.Controls
{
	public class JsonTreeView: TreeView
    {
        #region properties

        public string JsonFileName
		{
			get { return (string)GetValue(JsonFileNameProperty); }
			set { SetValue(JsonFileNameProperty, value); }
		}

		public string JsonText
		{
			get { return (string)GetValue(JsonTextProperty); }
			set { SetValue(JsonTextProperty, value); }
		}

		private static void OnJsonTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var obj = d as JsonTreeView;
			obj.PopulateTreeItems((string)e.NewValue);
		}
		
		private static void OnJsonFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var obj = d as JsonTreeView;

			var fileName = (string)e.NewValue;
			if (string.IsNullOrEmpty(fileName))
				return;

			if (fileName.EndsWith(".json") == false)
				return;

			if (fileName.StartsWith("zip:"))
				obj.JsonText = ReadZipFile(fileName);
			else
				obj.JsonText = File.ReadAllText(fileName);
		}

		public static readonly DependencyProperty JsonTextProperty = DependencyProperty.Register("JsonText", typeof(string), typeof(JsonTreeView), new PropertyMetadata("", OnJsonTextChanged));
		public static readonly DependencyProperty JsonFileNameProperty = DependencyProperty.Register("JsonFileName", typeof(string), typeof(JsonTreeView), new PropertyMetadata("", OnJsonFileNameChanged));

		#endregion
		#region serializers

		private Dictionary<string, object> DeserializeArrayList(string fileName)
		{
			try
			{
				JavaScriptSerializer serializer = new JavaScriptSerializer();

				var array = serializer.Deserialize<ArrayList>(JsonText) as ArrayList;
				serializer.MaxJsonLength = Int32.MaxValue;

				return ConvertArray(array);
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		private Dictionary<string, object> DeserializeDictionary(string fileName)
		{
			try
			{
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				serializer.MaxJsonLength = Int32.MaxValue;

				return serializer.Deserialize<Dictionary<string, object>>(JsonText) as Dictionary<string, object>;
			}
			catch (Exception)
			{
				return null;
			}
		}

        #endregion
        #region methods

        private static string ReadZipFile(string fileName)
		{
			var zipFileName = "";
			var subFileName = "";
			var mc = Regex.Match(fileName, @"^zip:(?<zipFileName>.*?),(?<subFileName>.*?)$");
			if (mc.Success)
			{
				zipFileName = mc.Groups["zipFileName"].Value;
				subFileName = mc.Groups["subFileName"].Value;
				var zipFile = ZipFile.OpenRead(zipFileName);
				foreach (var item in zipFile.Entries)
				{
					if (item.FullName == subFileName)
					{
						using (var reader = new StreamReader(item.Open()))
						{
							return reader.ReadToEnd();
						}
					}
				}
			}

			return string.Empty;
		}

		public void PopulateTreeItems(string text)
		{
			try
			{
				Items.Clear();

				var dics = DeserializeDictionary(text);
				if(dics!=null)
				{
					MakeNodes(dics, null);
				}
				else
				{
					var temp = DeserializeArrayList(text);
					if (temp != null)
					{
						MakeNodes(temp, null);
					}
					else
						throw new Exception("Error");
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		private void MakeNodes(Dictionary<string, object> data, TreeViewItem parent)
		{
			foreach(var key in data.Keys)
			{
				if(data[key] is Dictionary<string, object>)
				{
					var node = MakeTreeNode(key, parent, false);
					MakeNodes(data[key] as Dictionary<string, object>, node);
				}
				else if (data[key] is ArrayList)
				{
					var node = MakeTreeNode(key, parent, false);
					MakeNodes(ConvertArray(data[key]), node);
				}
				else
				{
					 MakeTreeNode($"{key} : \"{data[key]}\"", parent, true);
				}
			}
		}

		private TreeViewItem MakeTreeNode(string header, TreeViewItem parent, bool isLeaf = false)
		{
			var node = new TreeViewItem();
			var panel = new StackPanel()
			{
				Orientation = Orientation.Horizontal
			};
			var image = new Image()
			{
				Width = 16,
				Height = 16,
			};

			if (isLeaf)
				image.Source = new BitmapImage(new Uri("/Images/leaf.png", UriKind.Relative));
			else
				image.Source = new BitmapImage(new Uri("/Images/node.png", UriKind.Relative));

			var text = new TextBlock()
			{
				Text = header,
				FontSize = 10,
				VerticalAlignment = System.Windows.VerticalAlignment.Center,
				Margin = new System.Windows.Thickness(3, 1, 1, 1)
			};

			panel.Children.Add(image);
			panel.Children.Add(text);

			node.Header = panel;

			if (parent == null)
				Items.Add(node);
			else
				parent.Items.Add(node);
			return node;
		}

		private Dictionary<string, object> ConvertArray(object array)
		{
			var index = 1;
			var dics = new Dictionary<string, object>();
			foreach (var item in array as ArrayList)
			{
				dics.Add(index.ToString(), item);
				index++;
			}
			return dics;
		}

		#endregion
	}
}

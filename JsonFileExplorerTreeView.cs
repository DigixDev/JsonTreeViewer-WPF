using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Comae.Controls
{
    public class JsonFileExplorerTreeView : TreeView
    {
        public string RootFolder
        {
            get { return (string)GetValue(RootFolderProperty); }
            set { SetValue(RootFolderProperty, value); }
        }

        public string SelectedFileName
        {
            get { return (string)GetValue(SelectedFileNameProperty); }
            set { SetValue(SelectedFileNameProperty, value); }
        }

        public static readonly DependencyProperty SelectedFileNameProperty = DependencyProperty.Register("SelectedFileName", typeof(string), typeof(JsonFileExplorerTreeView), new PropertyMetadata(""));
        public static readonly DependencyProperty RootFolderProperty = DependencyProperty.Register("RootFolder", typeof(string), typeof(JsonFileExplorerTreeView), new PropertyMetadata("", OnDataChanged));

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = d as JsonFileExplorerTreeView;
            p.StartBrowsing(e.NewValue?.ToString());
        }

        private void StartBrowsing(string path)
        {
            Items.Clear();

            if (string.IsNullOrEmpty(path) == false)
                PopulateDirectoryList(path, null);
        }

        private void PopulateDirectoryList(string path, TreeViewItem parent)
        {
            var dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs)
            {
                var node = MakeNode(dir.ToLower(), parent, true);
                PopulateDirectoryList(dir, node);
            }

            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var name = file.ToLower();
                if (name.EndsWith(".json"))
                    MakeNode(name.ToLower(), parent, false);
                else if(name.EndsWith(".zip"))
                {
                    var node = MakeNode(name.ToLower(), parent, false);
                    using (var zipFile = ZipFile.OpenRead(name))
                    {
                        foreach (var item in zipFile.Entries)
                            MakeComplexNodes(item.FullName, name,  node);
                    }
                }
            }
        }

        private void MakeComplexNodes(string fullName, string zipFileName, TreeViewItem parent)
        {
            TreeViewItem temp = null;
            var node = parent;
            var parts = fullName.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                if (i == parts.Length - 1)
                {
                    var child = MakeNode(fullName, node, false, zipFileName);
                }
                else
                {
                    temp = null;
                    foreach (TreeViewItem item in node.Items)
                    {
                        var tag = item.Tag as dynamic;
                        if (tag.Name.Equals(parts[i]))
                        {
                            temp = item;
                            break;
                        }
                    }
                    if (temp != null)
                        node = temp;
                    else
                    {
                        node = MakeNode(parts[i], node, true);
                    }
                }
            }
        }

        private TreeViewItem MakeNode(string fullName ,TreeViewItem parent, bool isDirectory,  string zipFileName="" )
        {
            var name = "";
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

            if (isDirectory)
            {
                image.Source = new BitmapImage(new Uri("/Images/folder.png", UriKind.Relative));
                name = fullName.Substring(fullName.LastIndexOf('\\') + 1);
            }
            else
            {
                if (fullName.EndsWith(".json"))
                    image.Source = new BitmapImage(new Uri("/Images/json.png", UriKind.Relative));
                else
                    image.Source = new BitmapImage(new Uri("/Images/rar.png", UriKind.Relative));
                name = Path.GetFileName(fullName);
            }

            var text = new TextBlock()
            {
                Text = name,
                FontSize = 10,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new System.Windows.Thickness(3, 1, 1, 1)
            };

            panel.Children.Add(image);
            panel.Children.Add(text);

            node.Header = panel;
            node.Tag = new
            {
                Name = name,
                FullName = fullName,
                IsDirectory = isDirectory,
                ZipFileName = zipFileName
            };

            node.Cursor = Cursors.Hand;

            if (parent == null)
                Items.Add(node);
            else
                parent.Items.Add(node);
            return node;
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            var item = ((TreeViewItem)e.NewValue);
            var tag = item.Tag as dynamic;
            if (tag.IsDirectory == false)
            {
                item.Cursor = Cursors.Wait;
                if (String.IsNullOrEmpty(tag.ZipFileName))
                    SelectedFileName = tag.FullName;
                else
                    SelectedFileName = $"zip:{tag.ZipFileName},{tag.FullName}";
                item.Cursor = Cursors.Hand;
            }
        }

        public JsonFileExplorerTreeView()
        {

        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BsaBrowser.Views
{
    /// <summary>
    /// Interaction logic for MainContent.xaml
    /// </summary>
    public partial class MainContent : Page
    {
        public MainContent()
        {
            InitializeComponent();
        }

        private Models.MainViewModel Model
        {
            get => DataContext as Models.MainViewModel;
        }

        private void List_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            if (Model != null)
            {
                Model.OnFileDrop(e);
            }
        }

        private void TreeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model != null && treeList.SelectedItem is Archive.ArchiveNode node)
            {
                Model.TreeSelected = node;
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Model != null && listView.SelectedItem is Archive.ArchiveNode node && node.IsFolder)
            {
                if (Model.TreeSelected != null)
                {
                    Model.TreeSelected.IsExpanded = true;
                    Model.TreeSelected.IsSelected = false;
                }

                node.IsSelected = true;
                Model.TreeSelected = node;
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model != null)
            {
                var nodes = new List<Archive.ArchiveNode>();
                foreach (var item in listView.SelectedItems)
                {
                    if (item is Archive.ArchiveNode n)
                    {
                        nodes.Add(n);
                    }
                }
                Model.SelectedNodes = nodes;
            }
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            int charCodeDiff = (int)'a' - (int)Key.A;
            int numCodeDiff = (int)'1' - (int)Key.D0;
            int numpadCodeDiff = (int)'1' - (int)Key.NumPad0;
            char keyChar = '\0';

            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                keyChar = (char)(charCodeDiff + (int)e.Key);
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                keyChar = (char)(numCodeDiff + (int)e.Key);
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                keyChar = (char)(numpadCodeDiff + (int)e.Key);
            }

            if (keyChar != '\0' && Model != null)
            {
                var node = Model.Nodes.Where(n => n.Name.StartsWith(keyChar)).FirstOrDefault();
                if (node != null && sender is ListView listView)
                {
                    listView.ScrollIntoView(node);
                    if (listView.SelectedItems.Count == 0)
                    {
                        listView.SelectedItem = node;
                    }
                }
            }
        }
    }
}

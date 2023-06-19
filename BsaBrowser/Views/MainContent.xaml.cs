using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    }
}

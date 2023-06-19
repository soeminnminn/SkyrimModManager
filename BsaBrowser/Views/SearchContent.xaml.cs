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
    /// Interaction logic for SearchContent.xaml
    /// </summary>
    public partial class SearchContent : Page
    {
        public SearchContent()
        {
            InitializeComponent();
        }

        private Models.MainViewModel Model
        {
            get => DataContext as Models.MainViewModel;
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

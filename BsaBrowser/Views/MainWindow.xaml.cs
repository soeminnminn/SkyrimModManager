using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        public Models.MainViewModel model = new Models.MainViewModel();
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            DataContext = model;
        }
        #endregion

        #region Methods
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                var filePath = args.Where(f => f.ToLowerInvariant().EndsWith(".bsa") || f.ToLowerInvariant().EndsWith(".ba2")).ToArray().FirstOrDefault();
                if (!string.IsNullOrEmpty(filePath))
                    model.OpenFile(filePath);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (model.IsProcessing)
            {
                e.Cancel = true;
            }
            else
            {
                model.Dispose();
            }
            base.OnClosing(e);
        }
        #endregion

        #region Events Methods
        private void Window_DragEnter(object sender, DragEventArgs e)
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            model.OnFileDrop(e);
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            model.OpenFile();
        }

        private void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.model.IsFileOpened;
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.model.CloseFile();
        }

        private void CopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = model.HasSelected;
        }

        private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            model.CopySelected();
        }

        private void FindCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.model.IsHasFiles;
        }

        private void FindCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            searchBox.Focus();
        }

        private void ExtractCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = model.HasSelected;
        }

        private void ExtractCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            model.Extract();
        }

        private void ExtractAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = model.IsFileOpened;
        }

        private void ExtractAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            model.ExtractAll();
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog()
            {
                Owner = this
            };
            aboutDialog.ShowDialog();
        }
        #endregion
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ModManager.Models;
using ModManager.ViewModels;

namespace ModManager
{
    /// <summary>
    /// Interaction logic for BackupWindow.xaml
    /// </summary>
    public partial class BackupWindow : Window
    {
        private BackupFilesModel? viewModel;

        public BackupWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            var config = Config.Load();
            this.viewModel = new BackupFilesModel(config);
            if (this.viewModel.Load())
            {
                this.DataContext = this.viewModel;
            }
        }

        private void listFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != null)
            {
                var item = e.AddedItems[0] as FileModel;
                if (item != null)
                {
                    this.txtContent.Text = this.viewModel?.GetContent(item) ?? "";
                }
            }
        }

        private void RestoreCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listFiles.SelectedItem != null)
            {
                var item = listFiles.SelectedItem as FileModel;
                if (item != null && this.viewModel?.Restore(item) == true)
                {
                    MessageBox.Show(this, LocalizedStrings.MessageRestoreString, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listFiles.SelectedItem != null)
            {
                var item = listFiles.SelectedItem as FileModel;
                if (item != null && this.viewModel?.Delete(item) == true)
                {
                    this.txtContent.Text = string.Empty;
                }
            }
        }
    }
}

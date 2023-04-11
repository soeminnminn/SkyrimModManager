using System;
using System.Windows;
using ModManager.ViewModels;
using ModManager.Models;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading;
using System.Windows.Controls;

namespace ModManager.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;
        private BackgroundWorker workerLoad;
        private BackgroundWorker workerRestore;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }
            
        private void Initialize()
        {
            var config = Config.Load();

            this.viewModel = new MainViewModel(config);
            this.DataContext = viewModel;

            this.workerLoad = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
            };
            this.workerLoad.DoWork += WorkerLoad_DoWork;
            this.workerLoad.RunWorkerCompleted += WorkerLoad_RunWorkerCompleted;

            this.workerRestore = new BackgroundWorker()
            {
                WorkerReportsProgress = true
            };
            this.workerRestore.DoWork += WorkerRestore_DoWork;
            this.workerRestore.RunWorkerCompleted += WorkerRestore_RunWorkerCompleted;

            this.ReloadData();
        }

        private void EnableControls(bool enabled)
        {
            ModsListBox.IsEnabled = enabled;
        }

        private void ReloadData()
        {
            if (this.workerLoad?.IsBusy == false)
            {
                this.statusMessage.Text = LocalizedStrings.MessageLoadingString;
                this.EnableControls(false);
                this.workerLoad?.RunWorkerAsync(this.viewModel);
            }
        }

        private void WorkerLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.statusMessage.Text = LocalizedStrings.MessageReadyString;
            this.EnableControls(true);

            if ((e.Result as bool?) != true)
            {
#if !DEBUG
                if (MessageBox.Show(this, LocalizedStrings.MessageCantLoadString, this.Title, MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    this.Close();
                }
#endif
            }
        }

        private void WorkerLoad_DoWork(object sender, DoWorkEventArgs e)
        {
            var model = e.Argument as MainViewModel;
            e.Result = model?.Load();
            Thread.Sleep(100);
        }

        private void WorkerRestore_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.statusMessage.Text = LocalizedStrings.MessageReadyString;
            this.EnableControls(true);

            if ((e.Result as bool?) == true)
            {
                MessageBox.Show(this, LocalizedStrings.MessageRestoreString, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void WorkerRestore_DoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = e.Argument as string;
            e.Result = this.viewModel?.Restore(fileName);
            Thread.Sleep(100);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.viewModel?.HasChanged == true)
            {
                var result = MessageBox.Show(this, LocalizedStrings.MessageNeedSaveString, this.Title,
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                    case MessageBoxResult.Yes:
                        this.viewModel.Save();
                        break;
                    default:
                        break;
                }
            }
            base.OnClosing(e);
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.viewModel?.HasChanged == true && this.workerLoad?.IsBusy == false;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.viewModel?.Save() == true)
            {
                MessageBox.Show(this, LocalizedStrings.MessageSaveString, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Refresh_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.workerLoad?.IsBusy == false;
        }

        private void RefreshCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.ReloadData();
        }

        private void Backup_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.viewModel?.IsValid == true && this.workerLoad?.IsBusy == false;
        }

        private void BackupCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.viewModel?.Backup() == true)
            {
                MessageBox.Show(this, LocalizedStrings.MessageBackupString, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RestoreCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var backupWindow = new BackupWindow();
            backupWindow.Owner = this;
            if (backupWindow.ShowDialog() == true)
            {
                this.ReloadData();
            }
        }

        private void RestoreLast_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = this.workerRestore?.IsBusy == false;
        }

        private void RestoreLastCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.workerRestore?.IsBusy == false)
            {
                this.workerRestore.RunWorkerAsync();
            }
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new AboutDialog()
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private void ModsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ModsListBox.SelectedItem as ListItemModel;
            if (item != null && item.Info != null)
            {
                var recordFlags = string.Empty;
                if (item.Info.HasLightFlag)
                    recordFlags += "ESL" + Environment.NewLine;
                else if (item.Info.HasMasterFlag)
                    recordFlags += "ESM" + Environment.NewLine;
                else if (item.Info.ESU)
                    recordFlags += "ESU" + Environment.NewLine;
                
                if (item.Info.Localized)
                    recordFlags += "Localized" + Environment.NewLine;

                lblInfoName.Text = item.Info.Name;
                lblInfoAuthor.Text = item.Info.Author;
                lblInfoDesc.Text = item.Info.Description;
                lblInfoFlags.Text = recordFlags.TrimEnd();
                lblInfoMasters.Text = string.Join(Environment.NewLine, item.Info.Dependencies);
                lblInfoDate.Text = item.Info.DateTime.ToString("G");
                layoutInfo.Visibility = Visibility.Visible;
            }
            else
            {
                layoutInfo.Visibility = Visibility.Hidden;
            }
        }
    }
}

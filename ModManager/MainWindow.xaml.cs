using System;
using System.Windows;
using ModManager.ViewModels;
using ModManager.Models;
using System.ComponentModel;

namespace ModManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? viewModel;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }
            
        private void Initialize()
        {
            var config = Config.Load();

            this.viewModel = new MainViewModel(config);
            if (this.viewModel.Load())
            {
                this.DataContext = viewModel;
            }
            else
            {
                if (MessageBox.Show(this, "Can not read plugin config file.", this.Title, MessageBoxButton.OK) == MessageBoxResult.OK)
                {
                    this.Close();
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.viewModel?.HasChanged == true)
            {
                var result = MessageBox.Show(this, "Do you want to save changes", this.Title,
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

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (this.viewModel?.Save() == true)
            {
                MessageBox.Show(this, "Data has been save successfully.", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

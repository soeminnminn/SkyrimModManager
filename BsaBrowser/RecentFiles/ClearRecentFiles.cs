using System;
using System.Windows.Input;

namespace BsaBrowser.RecentFiles
{
    internal sealed class ClearRecentFiles : ICommand
    {
        private readonly Action _executeHandler;

        public ClearRecentFiles(Action execute) => _executeHandler = execute;
        
        public bool CanExecute(object parameter) => true;

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public void Execute(object parameter) => _executeHandler();
    }
}

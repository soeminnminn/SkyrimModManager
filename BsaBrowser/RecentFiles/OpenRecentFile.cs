using System;
using System.Windows.Input;

namespace BsaBrowser.RecentFiles
{
    internal sealed class OpenRecentFile : ICommand
    {
        private readonly Action<object> _executeHandler;

        public OpenRecentFile(Action<object> execute) => _executeHandler = execute;

        public bool CanExecute(object parameter) => true;

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public void Execute(object parameter) => _executeHandler(parameter);
    }
}

using System;
using System.Windows;

namespace ModManager.Plugin
{
    public interface IPlugin
    {
        string Name { get; }

        void OnPluginLoaded();

        void Execute(object parameter);
    }
}

using System;
using System.Windows;
using ModManager.Models;

namespace ModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal Config config;

        protected override void OnStartup(StartupEventArgs e)
        {
            config = Config.Load();

            foreach (var plugin in config.PluginTools)
            {
                if (plugin.IsExecutable)
                {
                    Plugin.Manager.Instance.AddExecutable(plugin.Name, plugin.Title, null);
                }
                else
                {
                    var info = new Plugin.PluginInfo(plugin.Name, plugin.Title, plugin.EntryType, plugin.Arguments);
                    if (plugin.EntryMethod != null)
                    {
                        info.SetEntryMethod(plugin.EntryMethod.Name, plugin.EntryMethod.IsStatic);
                    }
                    Plugin.Manager.Instance.Add(info);
                }
            }

            base.OnStartup(e);
        }
    }
}

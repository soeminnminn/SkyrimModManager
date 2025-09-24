using System;
using System.Collections.Generic;
using System.IO;

namespace ModManager.Plugin
{
    internal class Manager
    {
        #region Variables
        internal const string c_prefix = "Plugin.";
        private static readonly string c_workingDir = AppDomain.CurrentDomain.BaseDirectory;

        private static Manager instance = null;
        private readonly List<PluginCommand> commands;
        #endregion

        #region Constructor
        public Manager()
        {
            this.commands = new List<PluginCommand>();
            var assmInfos = AssmLoader.LoadDirectory(c_workingDir, $"{c_prefix}*.dll", typeof(IPlugin));

            foreach (var assm in assmInfos)
            {
                var cmd = new PluginCommand(assm, null);
                this.commands.Add(cmd);
            }
        }
        #endregion

        #region Methods
        public void Add(PluginInfo plugin)
        {
            if (string.IsNullOrEmpty(plugin.Name)) return;
            if (plugin.EntryType == null && plugin.EntryTypeName == null) return;

            var assmPath = Path.Combine(c_workingDir, plugin.Name);

            AssmInfo assm = null;
            if (plugin.EntryType == null)
            {
                assm = AssmLoader.LoadAssembly(assmPath, plugin.EntryTypeName);
            }
            else
            {
                assm = AssmLoader.LoadAssembly(assmPath, plugin.EntryType);
            }

            if (assm != null)
            {
                var cmd = new PluginCommand(plugin.Name, plugin.Title, plugin.Arguments, assm, plugin.EntryMethod);
                this.commands.Add(cmd);
            }
        }

        public void AddExecutable(string name, string title, string[] args)
        {
            if (string.IsNullOrEmpty(name)) return;

            var execPath = name;
            if (!File.Exists(execPath))
            {
                execPath = Path.Combine(c_workingDir, name);
            }

            if (File.Exists(execPath))
            {
                var cmd = new PluginCommand(execPath, title, args);
                this.commands.Add(cmd);
            }
        }
        #endregion

        #region Properties
        public List<PluginCommand> Plugins
        {
            get => this.commands;
        }
        #endregion

        #region Static
        public static Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Manager();
                }
                return instance;
            }
        }
        #endregion
    }
}

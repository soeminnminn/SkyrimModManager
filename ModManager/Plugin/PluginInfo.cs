using System;

namespace ModManager.Plugin
{
    public class PluginMethodInfo
    {
        public PluginMethodInfo(string name, bool isStatic) 
        {
            this.Name = name;
            this.IsStatic = isStatic;
        }   

        public string Name { get; private set; }

        public bool IsStatic { get; private set; }
    }

    public class PluginInfo
    {
        #region Variables
        private Type entryType = null;
        private string entryTypeName = null;
        private PluginMethodInfo entryMethod = null;
        #endregion

        #region Constructors
        public PluginInfo(string name, string title, string entryType, string[] args)
        {
            this.Name = name;
            this.Title = title;
            this.Arguments = args;
            this.entryTypeName = entryType;
        }

        public PluginInfo(string name, string title, string entryType, string[] args, PluginMethodInfo entryMethod)
        {
            this.Name = name;
            this.Title = title;
            this.Arguments = args;
            this.entryTypeName = entryType;
            this.entryMethod = entryMethod;
        }

        public PluginInfo(string name, string title, Type entryType, string[] args)
        {
            this.Name = name;
            this.Title = title;
            this.Arguments = args;
            this.entryType = entryType;
        }
        #endregion

        #region Properties
        public string Name { get; private set; }

        public string Title { get; private set; }

        public string EntryTypeName 
        {
            get => entryType != null ? entryType.FullName : this.entryTypeName;
        }

        public Type EntryType { get => this.entryType; }

        public PluginMethodInfo EntryMethod { get => this.entryMethod; }

        public string[] Arguments { get; private set; }
        #endregion

        #region Methods
        public void SetEntryMethod(string name, bool isStatic)
        {
            this.entryMethod = new PluginMethodInfo(name, isStatic);
        }
        #endregion
    }
}

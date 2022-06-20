using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModManager.GameModules;

namespace ModManager.Models
{
    public class ListItemModel : INotifyPropertyChanged
    {
        private bool mIsEnabled = false;

        private bool mIsFound = false;
        private bool mIsSystem = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ListItemModel()
        {
        }

        public ListItemModel(int index) 
            : this()
        {
            this.Index = index;
            this.Name = string.Format("Item {0}", index);
        }

        [JsonIgnore()]
        public int Index { get; set; } = -1;

        public string Name { get; set; } = string.Empty;

        public string ErrorMessage 
        { 
            get 
            {
                var messages = new List<string>();
                if (!IsFound)
                    messages.Add("File not found");
                if (MasterMissing)
                    messages.Add("Master file missing");
                return string.Join(", ", messages).Trim();
            } 
        }

        [JsonIgnore()]
        public PluginInfo? Info { get; set; }

        public bool HasError
        {
            get => !string.IsNullOrEmpty(ErrorMessage);
        }

        public bool IsEnabled
        {
            get => this.mIsEnabled;
            set
            {
                if (value == this.mIsEnabled) return;
                this.mIsEnabled = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsFound
        {
            get => this.mIsFound;
            set
            {
                if (value == this.mIsFound) return;
                this.mIsFound = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsSystem
        {
            get => this.mIsSystem;
            set
            {
                if (value == this.mIsSystem) return;
                this.mIsSystem = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsUser 
        {
            get => !this.IsSystem;
        }

        public bool MasterMissing 
        { 
            get => Info?.MissingMaster ?? false; 
        }

        public bool CanCheck 
        {
            get => IsFound && !MasterMissing;
        }

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}

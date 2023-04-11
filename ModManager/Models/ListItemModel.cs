using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using ModManager.GameModules;

namespace ModManager.Models
{
    public class ListItemModel : INotifyPropertyChanged
    {
        #region Variables
        private bool mIsEnabled = false;

        private bool mIsFound = false;
        private bool mIsSystem = false;

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructor
        public ListItemModel()
        {
        }

        public ListItemModel(int index) 
            : this()
        {
            this.Index = index;
            this.Name = string.Format("Item {0}", index);
        }
        #endregion

        #region Properties
        [JsonIgnore()]
        public int Index { get; set; } = -1;

        public string Name { get; set; } = string.Empty;

        public string ErrorMessage 
        { 
            get 
            {
                var message = string.Empty;
                if (!IsFound)
                    message = "File not found";
                else if (Info == null)
                    message = "File can't load";
                else if (MasterMissing)
                    message = "Master file missing";
                return message.Trim();
            } 
        }

        [JsonIgnore()]
        public PluginInfo Info { get; set; }

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
                this.OnPropertyChanged("IsEnabled");
            }
        }

        public bool IsFound
        {
            get => this.mIsFound;
            set
            {
                if (value == this.mIsFound) return;
                this.mIsFound = value;
                this.OnPropertyChanged("IsFound");
            }
        }

        public bool IsSystem
        {
            get => this.mIsSystem;
            set
            {
                if (value == this.mIsSystem) return;
                this.mIsSystem = value;
                this.OnPropertyChanged("IsSystem");
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
        #endregion

        #region Methods
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return this.Name;
        }
        #endregion
    }
}

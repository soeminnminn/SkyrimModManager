using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using ModManager.Models;

namespace ModManager.ViewModels
{
    public class MainViewModel : DefaultDropHandler, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = null;

        private Config? config;
        private Formatter? formatter;
        private FileInfo? mPluginFile;
        private string mOrigData = string.Empty;
        private bool mHasChanged = false;

        public MainViewModel() 
        {
            List<ListItemModel> list = new List<ListItemModel>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new ListItemModel
                {
                    Index = i,
                    Name = string.Format("Item {0}", i)
                });
            }

            this.Data = new ObservableCollection<ListItemModel>(list);
        }

        public MainViewModel(Config config)
        {
            this.config = config;
            this.formatter = new Formatter(config.Format!);
            this.Data = new ObservableCollection<ListItemModel>();
            this.Data.CollectionChanged += this.Data_CollectionChanged;
        }

        public bool Load()
        {
            if (this.config == null) return false;

            this.Data.CollectionChanged -= this.Data_CollectionChanged;
            this.Comments.Clear();

            this.mPluginFile =  config.GetPluginFile();
            if (this.mPluginFile != null)
            {
                var pluginData = new List<string>();
                using (var reader = new StreamReader(mPluginFile.OpenRead()))
                {
                    while(!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            pluginData.Add(line);
                        }
                    }
                }

                List<Formatter.Item> parsed = new List<Formatter.Item>(); 
                if (pluginData.Count > 0)
                {
                    parsed = this.formatter!.Parse(pluginData);
                }

                var dataDir = config.GetModulesDataDir();
                if (dataDir != null && dataDir.Exists)
                {
                    var exts = config.ModuleExtensions;
                    var systemMods = config.SystemModules;

                    var files = dataDir.EnumerateFiles();
                    var modules = files.Where(file =>
                    {
                        return exts.Contains(file.Extension.ToLower().Substring(1));
                    }).ToList().ConvertAll(f => f.Name);

                    modules.Sort();

                    var list = new List<ListItemModel>();
                    foreach(var sysMod in systemMods)
                    {
                        if (modules.Contains(sysMod))
                        {
                            list.Add(new ListItemModel 
                            {
                                Name = sysMod,
                                Index = list.Count,
                                IsSystem = true,
                                IsEnabled = true,
                                IsFound = true
                            });
                        }
                    }

                    foreach(var p in parsed)
                    {
                        if (p.IsComment)
                        {
                            if (!string.IsNullOrEmpty(p.Data))
                            {
                                this.Comments.Add(p.Data);
                            }                            
                            continue;
                        }
                        if (string.IsNullOrEmpty(p.Data)) continue;
                        if (systemMods.Contains(p.Data)) continue;

                        list.Add(new ListItemModel
                        {
                            Name = p.Data,
                            Index = list.Count,
                            IsSystem = false,
                            IsEnabled = p.IsEnabled,
                            IsFound = modules.Contains(p.Data)
                        });
                    }
                    
                    var namesList = list.ConvertAll(x => x.Name);
                    var notInList = modules.Where(x => !namesList.Contains(x)).ToList();
                    foreach(var m in notInList)
                    {
                        list.Add(new ListItemModel 
                        {
                            Name = m,
                            Index = list.Count,
                            IsSystem = false,
                            IsEnabled = false,
                            IsFound = true
                        });
                    }

                    this.mOrigData = JsonSerializer.Serialize(list);
                    this.Data = new ObservableCollection<ListItemModel>(list);
                    this.mHasChanged = false;
                    this.Data.CollectionChanged += this.Data_CollectionChanged;

                    return true;
                }
            }

            return false;
        }

        private void Data_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.mHasChanged = true;
        }

        public bool HasChanged
        {
            get {
                var list = this.Data.ToList();
                var d = JsonSerializer.Serialize(list);
                if (d != this.mOrigData) return true;
                return mHasChanged;
            }
        }

        public bool Save()
        {
            if (this.HasChanged && this.formatter != null && this.mPluginFile != null)
            {
                var list = Data.ToList();
                var result = this.formatter.Make(Comments, list);
                if (result.Count > 0)
                {
                    try
                    {
                        if (this.mPluginFile.Exists)
                        {
                            this.mPluginFile.Delete();
                        }

                        using(var stream = this.mPluginFile.CreateText())
                        {
                            foreach(var item in result)
                            {
                                if (string.IsNullOrEmpty(item)) continue;
                                stream.WriteLine(item);
                            }
                            stream.Flush();
                        }

                        this.mOrigData = JsonSerializer.Serialize(list);
                        this.mHasChanged = false;
                        return true;
                    }
                    catch (IOException)
                    { 
                    }
                }
            }
            return false;
        }

        public List<string> Comments { get; private set; } = new List<string>();

        public ObservableCollection<ListItemModel> Data { get; private set; }

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override void DragOver(IDropInfo dropInfo)
        {
            var targetItem = dropInfo.TargetItem as ListItemModel;
            if (targetItem != null && !targetItem.IsSystem)
            {
                base.DragOver(dropInfo);
            }
            else
            {
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        public override void Drop(IDropInfo dropInfo)
        {
            base.Drop(dropInfo);
        }
    }
}

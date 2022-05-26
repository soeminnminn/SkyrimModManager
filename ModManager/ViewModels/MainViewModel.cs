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
using ModManager.GameModules;

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
            this.formatter = new Formatter(true);
            this.Data = new ObservableCollection<ListItemModel>();
            this.Data.CollectionChanged += this.Data_CollectionChanged;
        }

        public bool Load()
        {
            if (this.config == null) 
            {
                this.mHasChanged = false;
                return false;
            }

            this.Data.CollectionChanged -= this.Data_CollectionChanged;
            this.Comments.Clear();

            this.mPluginFile = config.GetPluginFile();
            if (this.mPluginFile != null && config.Settings != null)
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
                    var modules = this.GetPlugins(dataDir, parsed);

                    var moduleNames = modules.Select(x => x.OriginalName).ToList();
                    var systemMods = config.Settings.ImplicitlyActivePlugins;

                    var list = new List<ListItemModel>();
                    foreach(var sysMod in systemMods)
                    {
                        if (moduleNames.Contains(sysMod))
                        {
                            var info = modules.Find(x => x.OriginalName == sysMod);
                            list.Add(new ListItemModel 
                            {
                                Name = sysMod,
                                Index = list.Count,
                                Info = info,
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

                        var info = modules.Find(x => x.OriginalName == p.Data);
                        list.Add(new ListItemModel
                        {
                            Name = p.Data,
                            Index = list.Count,
                            Info = info,
                            IsSystem = false,
                            IsEnabled = p.IsEnabled,
                            IsFound = moduleNames.Contains(p.Data)
                        });
                    }
                    
                    var namesList = list.ConvertAll(x => x.Name);
                    var notInList = moduleNames.Where(x => !namesList.Contains(x)).ToList();
                    foreach(var m in notInList)
                    {
                        var info = modules.Find(x => x.OriginalName == m);
                        list.Add(new ListItemModel 
                        {
                            Name = m,
                            Index = list.Count,
                            Info = info,
                            IsSystem = false,
                            IsEnabled = false,
                            IsFound = true
                        });
                    }
                    list.Sort(new ListItemModelComparer());

                    this.mOrigData = JsonSerializer.Serialize(list);
                    this.Data = new ObservableCollection<ListItemModel>(list);
                    this.mHasChanged = false;
                    this.Data.CollectionChanged += this.Data_CollectionChanged;

                    return true;
                }
            }

            return false;
        }

        private List<PluginInfo> GetPlugins(DirectoryInfo dataDir, List<Formatter.Item> parsedTxt)
        {
            var list = new List<PluginInfo>();
            if (this.config != null && dataDir != null && dataDir.Exists)
            {
                var gameId = this.config.GameId;
                var parsed = parsedTxt.Where(x => !x.IsComment).ToList();

                var files = dataDir.EnumerateFiles();
                var modules = files.Where(file => this.config!.Settings!.IsValidExtension(file.Name));
                var systemMods = this.config!.Settings!.HardcodedPlugins;

                foreach (var m in modules)
                {
                    var name = GameSettings.UnGhost(m.Name);
                    if (systemMods.Contains(name)) continue;

                    var plugin = new PluginFile(gameId, m);
                    plugin.Parse(true, false);
                    if (plugin.IsValid)
                    {
                        var idx = parsed.FindIndex(x => x.Data == m.Name);
                        var info = new PluginInfo(plugin, this.config!.Settings!, idx);
                        list.Add(info);
                    }
                }
            }
            return list;
        }

        private void Data_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.mHasChanged = true;
        }

        public bool HasChanged
        {
            get {
                if (string.IsNullOrEmpty(this.mOrigData)) return false;
                
                var list = this.Data.ToList();
                var d = JsonSerializer.Serialize(list);
                if (d != this.mOrigData) return true;
                return mHasChanged;
            }
        }

        public bool Save()
        {
#if DEBUG
            return false;
#else
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
#endif
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
    
        public class ListItemModelComparer : IComparer<ListItemModel>
        {
            public int Compare(ListItemModel? a, ListItemModel? b)
            {
                var result = 0;
                if (a != null && b == null) result = -1;
                if (a == null && b != null) result = 1;

                if (result == 0 && a != null && b != null)
                {
                    if (a.Info != null && b.Info == null) result = -1;
                    if (a.Info == null && b.Info != null) result = 1;

                    if (result == 0 && a.Info != null) result = a.Info.CompareTo(b.Info);
                }                
                return result;
            }
        }
    }
}

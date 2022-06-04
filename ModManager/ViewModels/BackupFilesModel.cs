using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using ModManager.Models;

namespace ModManager.ViewModels
{
    internal class BackupFilesModel
    {
        public ObservableCollection<FileModel> Data { get; private set; }
        private Config? config;

        public BackupFilesModel()
        {
            this.Data = new ObservableCollection<FileModel>();
        }

        public BackupFilesModel(Config config)
            : this()
        {
            this.config = config;
        }

        public bool Load()
        {
            var configDir = Config.AppDataConfigDir;
            if (!string.IsNullOrEmpty(configDir) && !string.IsNullOrEmpty(this.config?.Name))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(Path.Combine(configDir, "Backup", this.config.Name));
                    if (!dirInfo.Exists)
                    {
                        return false;
                    }

                    var files = dirInfo.GetFiles("*.txt");
                    var list = new List<FileModel>();
                    foreach (var file in files)
                    {
                        list.Add(new FileModel(file));
                    }

                    list.Sort(delegate (FileModel a, FileModel b) {
                        return b.Name.CompareTo(a.Name);
                    });
                    this.Data = new ObservableCollection<FileModel>(list);

                    return true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            return false;
        }

        public bool Delete(FileModel file)
        {
            if (File.Exists(file.Path))
            {
                try
                {
                    File.Delete(file.Path);
                    this.Data.Remove(file);
                    return true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            return false;
        }

        public bool Restore(FileModel file)
        {
            if (File.Exists(file.Path))
            {
                try
                {
                    var pluginFilePath = this.config?.PluginFile;
                    if (!string.IsNullOrEmpty(pluginFilePath))
                    {
                        File.Copy(file.Path, pluginFilePath, true);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            return false;
        }

        public string GetContent(FileModel model)
        {
            if (File.Exists(model.Path))
            {
                try
                {
                    return File.ReadAllText(model.Path);
                } 
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
                
            }
            return string.Empty;
        }
    }
}

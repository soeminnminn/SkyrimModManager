using System;
using System.Collections.Generic;
using System.IO;

namespace ModManager.Models
{
    internal class FileModel
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;

        public FileModel()
        {
        }

        public FileModel(FileInfo file)
        {
            this.Name = file.Name;
            this.Path = file.FullName;
            this.Date = file.LastWriteTime.ToString("g");
        }

        public static FileModel Fake()
        {
            var fileModel = new FileModel();
            fileModel.Name = "File " + Random.Shared.Next(10).ToString();
            fileModel.Date = DateTime.Now.ToString("g");

            return fileModel;
        }
    }
}

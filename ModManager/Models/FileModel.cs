using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ModManager.Models
{
    internal class FileModel
    {
        #region Variables
        private static Regex namePattern = new(@"^([\d]{2})-([\d]{2})-([\d]{4})_([\d]{2})([\d]{2})([\d]{2})([\d]{3})");
        #endregion

        #region Properties
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;
        #endregion

        #region Constructor
        public FileModel()
        {
        }

        public FileModel(FileInfo file)
        {
            this.Name = file.Name;
            this.Path = file.FullName;
            this.Date = file.LastWriteTime.ToString("g");
        }
        #endregion

        #region Methods
        public static FileModel Fake()
        {
            var fileModel = new FileModel();
            fileModel.Name = "File " + Random.Shared.Next(10).ToString();
            fileModel.Date = DateTime.Now.ToString("g");

            return fileModel;
        }

        public override string ToString()
        {
            var match = namePattern.Match(Name);
            if (match.Success)
            {
                var dateStr = string.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}", match.Groups[1], match.Groups[2], match.Groups[3], match.Groups[4], match.Groups[5], match.Groups[6], match.Groups[7]);
                try
                {
                    var date = DateTime.Parse(dateStr);
                    return date.ToString();
                }
                catch(FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    return dateStr;
                }                
            }
            return Name;
        }
        #endregion
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class PluginFile
    {
        public FileInfo? File = null;
        
        public GameID GameId { get; private set; }

        public Record? Header { get; private set; }

        public bool IsValid
        {
            get => this.Header != null && this.Header.RecordType.IsValid;
        }

        public List<IRecord> Records = new List<IRecord>();

        public string[] Dependencies = new string[] {};

        private bool m_isLightExt = false;
        public bool IsLight
        {
            get {
                return (this.Header != null && ((this.Header.Flags & 0x200) != 0)) ||
                        this.m_isLightExt;
            }
        }
        
        public PluginFile(GameID gameId, string path)
        {
            this.GameId = gameId;
            if (!string.IsNullOrEmpty(path))
            {
                this.File = new FileInfo(path);
                this.m_isLightExt = GameSettings.UnGhost(this.File.Name).ToLower().EndsWith(".esl");
            }
        }

        public PluginFile(GameID gameId, FileInfo file)
        {
            this.GameId = gameId;
            this.File = file;
            this.m_isLightExt = GameSettings.UnGhost(file.Name).ToLower().EndsWith(".esl");
        }

        public void Parse(bool headerOnly = true, bool parseSubRecords = false)
        {
            if (this.File == null || !this.File.Exists) return;

            try
            {
                using (var stream = this.File.OpenRead())
                {
                    var signature = Signature.ReadFrom(stream);
                    if (signature.IsValid && signature.IsHeader)
                    {
                        this.Header = new Record(this.GameId, signature, stream, false, true);
                        if (!headerOnly)
                        {
                            while(stream.Position < stream.Length)
                            {
                                signature = Signature.ReadFrom(stream);
                                if (signature.IsValid)
                                {
                                    if (signature.IsSubRecord)
                                    {
                                        var record = new SubRecord(this.GameId, signature, stream, 0, this.Header.Compressed);
                                        this.Records.Add(record);
                                    }
                                    else
                                    {
                                        var record = new Record(this.GameId, signature, stream, this.Header.Compressed, parseSubRecords);
                                        this.Records.Add(record);
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("Something wrong!");
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            var header = this.Header as Record;
            if (header != null)
            {
                var list = header.Records.Where(x => x.RecordType == "MAST" && x is SubRecord)
                                .Select(x => (x as SubRecord)!.AsString()).ToList();
                this.Dependencies = list.ToArray();
            }
        }
    }
}
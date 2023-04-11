using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ModManager.GameModules
{
    public class PluginFile
    {
        public FileInfo File = null;
        
        public GameID GameId { get; private set; }

        public Record Header { get; private set; }

        public bool IsValid
        {
            get => this.Header != null && this.Header.RecordType.IsValid;
        }

        public List<IRecord> Records = new List<IRecord>();

        public string Author { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public string[] Dependencies = new string[] {};

        public HEDR? HEDRData { get; private set; }

        public bool HasLightFlag
        {
            get {
                return (this.Header != null && ((this.Header.Flags & 0x200) != 0));
            }
        }

        public bool HasMasterFlag
        {
            get
            {
                return (this.Header != null && ((this.Header.Flags & 0x1) != 0));
            }
        }

        public bool Localized
        {
            get
            {
                return (this.Header != null && ((this.Header.Flags & 0x00000080) != 0));
            }
        }

        public PluginFile(GameID gameId, string path)
        {
            this.GameId = gameId;
            if (!string.IsNullOrEmpty(path))
            {
                this.File = new FileInfo(path);
            }
        }

        public PluginFile(GameID gameId, FileInfo file)
        {
            this.GameId = gameId;
            this.File = file;
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
                            while (stream.Position < stream.Length)
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

                var header = this.Header as Record;
                if (header != null)
                {
                    var mastersList = header.Records.Where(x => x.RecordType == "MAST" && x is SubRecord)
                                    .Select(x => (x as SubRecord)!.AsString()).ToList();
                    this.Dependencies = mastersList.ToArray();

                    this.Author = header.Records.Where(x => x.RecordType == "CNAM" && x is SubRecord)
                                    .Select(x => (x as SubRecord)!.AsString()).FirstOrDefault() ?? string.Empty;

                    this.Description = header.Records.Where(x => x.RecordType == "SNAM" && x is SubRecord)
                                    .Select(x => (x as SubRecord)!.AsString()).FirstOrDefault() ?? string.Empty;

                    var hedrRecord = header.Records.Where(x => x.RecordType == "HEDR" && x is SubRecord).FirstOrDefault() as SubRecord;
                    if (hedrRecord != null)
                    {
                        this.HEDRData = HEDR.Unpack(hedrRecord.Data);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12)]
        public struct HEDR
        {
            public float Version;
            public uint NumberOfRecords;
            public uint NextObjectId;

            internal static HEDR Unpack(byte[] bytes)
            {
                unsafe
                {
                    fixed (byte* map = &bytes[0])
                    {
                        return *(HEDR*)map;
                    }
                }
            }
        }
    }
}
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class Record : IRecord
    {
        public GameID GameId { get; }

        public Signature RecordType { get; set; } = Signature.DEFAULT;

        public uint DataSize { get; set; }

        public uint Flags { get; internal set; }

        public uint FormId { get; internal set; }

        public uint Revision { get; internal set; } = 0;

        public ushort Version { get; internal set; } = 0;

        public uint GroupType { get; internal set; } = 0;
        
        public ushort Stamp { get; internal set; } = 0;

        private bool m_isCompressed = false;
        public bool Compressed
        {
            get {
                if (this.RecordType.IsGroup)
                    return this.m_isCompressed;
                else 
                    return this.GameId != GameID.Morrowind && (this.Flags & 0x00040000) != 0;
            }
            set {
                this.m_isCompressed = value; 
             }
        }

        public bool IsMaster
        {
            get => (this.Flags & 0x1) != 0;
        }

        public bool Deleted
        {
            get {
                if (this.RecordType.IsGroup)
                    return this.GroupType == 6; // May be, I don't known.

                return (this.Flags & 0x20) != 0;
            }
        }

        public bool Ignored
        {
            get => (this.Flags & 0x1000) != 0;
        }

        public bool Blocked
        {
            get => this.GameId == GameID.Morrowind && (this.Flags & 0x00002000) != 0;
        }

        public bool Persistant
        {
            get => this.GameId == GameID.Morrowind && (this.Flags & 0x00000400) != 0;
        }

        public byte[] Data { get; set; } = new byte[] { };

        public int HeaderLength
        {
            get
            {
                if (this.GameId == GameID.Morrowind)
                    return 16;
                else if (this.GameId == GameID.Oblivion)
                    return 20;
                else
                    return 24;
            }
        }

        public List<IRecord> Records { get; private set; } = new List<IRecord>();

        public Record(GameID gameId, Signature signature)
        {
            this.GameId = gameId;
            this.RecordType = signature;
        }

        public Record(GameID gameId, Signature signature, Stream stream, bool compressed, bool parseSubRecords)
            : this(gameId, signature)
        {
            this.Compressed = compressed;
            this.Parse(stream, parseSubRecords);
        }

        internal void Parse(Stream stream, bool parseSubRecords)
        {
            var reader = new OBinaryReader(stream);

            if (this.RecordType.IsGroup)
                this.ParseGroupHeader(reader);
            else
                this.ParseRecordHeader(reader);

            if (this.DataSize < (stream.Length - stream.Position))
            {
                if (this.GroupType == 6)
                {
                    // No Data
                }
                else if (this.GroupType == 9)
                {
                    // Data size changed
                }
                else
                {
                    this.Data = reader.ReadBytes((int)this.DataSize);
                    if (parseSubRecords && !this.Deleted)
                        this.ParseSubRecords();
                }                
            }
        }

        private void ParseRecordHeader(OBinaryReader reader)
        {
            this.DataSize = reader.ReadUInt32();
            if (this.GameId == GameID.Morrowind) reader.Skip(4);
            this.Flags = reader.ReadUInt32();
            if (this.GameId != GameID.Morrowind) this.FormId = reader.ReadUInt32(); 
            if (this.GameId != GameID.Morrowind) this.Revision = reader.ReadUInt32(); // Version Control Info 1
            if (this.GameId != GameID.Morrowind && this.GameId != GameID.Oblivion)
            {
                this.Version = reader.ReadUInt16(); // Form Version
                reader.Skip(2); // Version Control Info 2
            }
        }

        private void ParseGroupHeader(OBinaryReader reader)
        {
            this.DataSize = reader.ReadUInt32();
            this.FormId = reader.ReadUInt32();
            this.GroupType = reader.ReadUInt32();
            this.Stamp = reader.ReadUInt16();
            if (this.GameId != GameID.Oblivion) reader.Skip(2);
            this.Version = reader.ReadUInt16();
            if (this.GameId != GameID.Oblivion) reader.Skip(2);
        }

        private void ParseSubRecords()
        {
            using (var stream = new MemoryStream(this.Data))
            {
                uint sizeOverride = 0;
                while (stream.Position < stream.Length)
                {
                    var signature = Signature.ReadFrom(stream);
                    if (signature.IsValid)
                    {
                        if (signature.IsGroup || this.RecordType.IsGroup)
                        {
                            var record = new Record(this.GameId, signature, stream, this.Compressed, true);
                            this.Records.Add(record);
                        }
                        else
                        {
                            var subRecord = new SubRecord(this.GameId, signature, stream, (int)sizeOverride, this.Compressed);
                            if (signature.IsXXXX)
                            {
                                if (subRecord.Data.Length >= 4)
                                {
                                    sizeOverride = BitConverter.ToUInt32(subRecord.Data.Take(4).ToArray());
                                }
                            }
                            else
                            {
                                sizeOverride = 0;
                                this.Records.Add(subRecord);
                            }
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

        public override string ToString()
        {
            return string.Format("{0}:{{ Size: {1}, DSize: {2}, GType: {3}, Compressed: {4} }}", 
                this.RecordType, this.DataSize, this.Data.Length, this.GroupType, this.Compressed);
        }

        public void CopyTo(IRecord other)
        {
            if (other != null && other.GameId == this.GameId)
            {
                other.RecordType = this.RecordType;
                other.DataSize = this.DataSize;
                other.Compressed = this.Compressed;
                if (Data.Length > 0)
                {
                    other.Data = new byte[this.Data.Length];
                    this.Data.CopyTo(other.Data, 0);
                }

                if (other is Record)
                {
                    (other as Record)!.Flags = this.Flags;
                    (other as Record)!.FormId = this.FormId;
                    (other as Record)!.Revision = this.Revision;
                    (other as Record)!.Version = this.Version;
                    (other as Record)!.GroupType = this.GroupType;
                    (other as Record)!.Stamp = this.Stamp;
                    if (this.Records.Count > 0)
                        (other as Record)!.Records.AddRange(this.Records);
                }
            }
        }
    }
}
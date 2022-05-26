using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class SubRecord : IRecord
    {
        public GameID GameId { get; }
        public Signature RecordType { get; set; } = Signature.DEFAULT;
        public uint DataSize { get; set; }

        public bool Compressed { get; set; } = false;

        public byte[] Data { get; set; } = new byte[] { };

        public byte[] DeCompressedData
        {
            get
            {
                if (this.Compressed && this.Data.Length > 4)
                {
                    var lenBytes = new byte[4];
                    this.Data.CopyTo(lenBytes, 0);
                    var uncLength = BitConverter.ToUInt32(lenBytes);
                    var len = this.Data.Length;

                    if (len < uncLength + 4) return this.Data;

                    var remainBytes = new byte[len - 4];
                    this.Data.CopyTo(remainBytes, 4);

                    using (var stream = new MemoryStream())
                    {
                        using (var dst = new MemoryStream(remainBytes))
                        {
                            using (DeflateStream src = new DeflateStream(dst, CompressionMode.Decompress))
                            {
                                src.CopyTo(stream);
                                return stream.ToArray();
                            }
                        }
                    }

                }
                return this.Data;
            }
        }

        public SubRecord(GameID gameId, Signature signature)
        {
            this.GameId = gameId;
            this.RecordType = signature;
        }

        public SubRecord(GameID gameId, Signature signature, Stream stream, int sizeOverride, bool compressed)
            : this(gameId, signature)
        {
            this.Compressed = compressed;
            this.Parse(stream, sizeOverride);
        }

        internal void Parse(Stream stream, int sizeOverride)
        {
            var reader = new OBinaryReader(stream);

            if (this.GameId == GameID.Morrowind)
                this.ParseMorrowind(reader);
            else if (sizeOverride != 0)
                this.ParsePresized(reader, sizeOverride);
            else
                this.ParseSimple(reader);
        }

        private void ParseMorrowind(OBinaryReader reader)
        {
            this.DataSize = reader.ReadUInt32();
            if (this.DataSize < (reader.Length - reader.Position))
                this.Data = reader.ReadBytes((int)this.DataSize);
        }

        private void ParsePresized(OBinaryReader reader, int size)
        {
            this.DataSize = reader.ReadUInt16();
            if (this.DataSize < (reader.Length - reader.Position))
                this.Data = reader.ReadBytes(size);
        }

        private void ParseSimple(OBinaryReader reader)
        {
            this.DataSize = reader.ReadUInt16();
            if (this.DataSize < (reader.Length - reader.Position))
                this.Data = reader.ReadBytes((int)this.DataSize);
        }

        public override string ToString()
        {
            return string.Format("{0} - Size = {1}", this.RecordType, this.DataSize);
        }

        public void CopyTo(IRecord other)
        {
            if (other != null && other.GameId == this.GameId)
            {
                other.RecordType = this.RecordType;
                other.Compressed = this.Compressed; 
                other.DataSize = this.DataSize;
                if (this.Data.Length > 0)
                {
                    other.Data = new byte[this.Data.Length];
                    this.Data.CopyTo(other.Data, 0);
                }
            }
        }

        public string AsString()
        {
            if (this.Data.Length > 0)
            {
                string str = Encoding.ASCII.GetString(this.Data);
                if (str.Length > 0 && str[str.Length - 1] == '\0')
                    return str.Substring(0, str.Length - 1);
                return str;
            }
            return string.Empty;
        }

        public uint[] AsUInt32Array()
        {
            if (this.Data.Length > 0)
            {
                var len = this.Data.Length;
                if ((len % 4) == 0)
                {
                    var list = new List<uint>();
                    for (int i = 0; i<len; i+=4)
                    {
                        var u = BitConverter.ToUInt32(
                            new byte[] {
                                Data[i + 0],
                                Data[i + 1],
                                Data[i + 2],
                                Data[i + 3]
                            }
                        );
                        list.Add(u);
                    }
                    return list.ToArray();
                }
            }
            return new uint[] {};
        }
    }
}
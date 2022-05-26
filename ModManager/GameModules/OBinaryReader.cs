using System;
using System.IO;
using System.Text;
using System.Buffers.Binary;

namespace ModManager.GameModules
{
    public class OBinaryReader : IDisposable
    {
        private readonly BinaryReader m_reader;

        public OBinaryReader(Stream input)
          : this(input, new UTF8Encoding(), true)
        {
        }

        public OBinaryReader(Stream input, Encoding encoding)
         : this(input, encoding, true)
        {
        }

        public OBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        {
            this.m_reader = new BinaryReader(input, encoding, leaveOpen);
        }

        public OBinaryReader(byte[] bytes)
          : this(bytes, new UTF8Encoding())
        {
        }

        public OBinaryReader(byte[] bytes, Encoding encoding)
          : this(new MemoryStream(bytes), encoding, false)
        {
        }

        public virtual Stream BaseStream
        {
            get
            {
                return this.m_reader.BaseStream;
            }
        }

        public virtual long Length
        {
            get => this.m_reader.BaseStream.Length;
        }

        public virtual long Position
        {
            get => this.m_reader.BaseStream.Position;
        }

        public virtual bool EndOfStream
        {
            get
            {
                long len = this.m_reader.BaseStream.Length;
                long pos = this.m_reader.BaseStream.Position;
                return len <= pos + 1;
            }
        }

        public virtual bool CanRead
        {
            get => this.m_reader.BaseStream.CanRead;
        }

        public virtual bool CanSeek
        {
            get => this.m_reader.BaseStream.CanSeek;
        }

        public virtual void Close()
        {
            this.m_reader.Close();
        }

        public void Dispose()
        {
            this.m_reader.Dispose();
        }

        public virtual int PeekChar() =>
          this.m_reader.PeekChar();

        public virtual int Read() =>
          this.m_reader.Read();

        public virtual bool ReadBoolean() =>
          this.m_reader.ReadBoolean();

        public virtual byte ReadByte() =>
          this.m_reader.ReadByte();

        public virtual sbyte ReadSByte() =>
          this.m_reader.ReadSByte();

        public virtual char ReadChar() =>
          this.m_reader.ReadChar();

        public virtual short ReadInt16() =>
          this.m_reader.ReadInt16();

        public virtual ushort ReadUInt16() =>
          this.m_reader.ReadUInt16();

        public virtual int ReadInt32() =>
          this.m_reader.ReadInt32();

        public virtual uint ReadUInt32() =>
          this.m_reader.ReadUInt32();

        public virtual long ReadInt64() =>
          this.m_reader.ReadInt64();

        public virtual ulong ReadUInt64() =>
          this.m_reader.ReadUInt64();

        public virtual float ReadSingle() =>
          this.m_reader.ReadSingle();

        public virtual double ReadDouble() =>
          this.m_reader.ReadDouble();

        public virtual decimal ReadDecimal() =>
          this.m_reader.ReadDecimal();

        public virtual double ReadDoubleBE()
        {
            return BinaryPrimitives.ReadDoubleBigEndian(this.m_reader.ReadBytes(8));
        }

        public virtual short ReadInt16BE()
        {
            return BinaryPrimitives.ReadInt16BigEndian(this.m_reader.ReadBytes(2));
        }

        public virtual int ReadInt32BE() =>
          BinaryPrimitives.ReadInt32BigEndian(this.m_reader.ReadBytes(4));

        public virtual long ReadInt64BE() =>
          BinaryPrimitives.ReadInt64BigEndian(this.m_reader.ReadBytes(8));

        public virtual float ReadSingleBE() =>
          BinaryPrimitives.ReadSingleBigEndian(this.m_reader.ReadBytes(4));

        public virtual ushort ReadUInt16BE() =>
          BinaryPrimitives.ReadUInt16BigEndian(this.m_reader.ReadBytes(2));

        public virtual uint ReadUInt32BE() =>
          BinaryPrimitives.ReadUInt32BigEndian(this.m_reader.ReadBytes(4));

        public virtual ulong ReadUInt64BE() =>
          BinaryPrimitives.ReadUInt64BigEndian(this.m_reader.ReadBytes(8));

        public virtual String ReadString() =>
          this.m_reader.ReadString();

        public virtual String ReadStringNS()
        {
            StringBuilder builder = new StringBuilder();
            char endChar = '\0';
            char c = endChar;
            while ((c = this.m_reader.ReadChar()) != endChar)
            {
                builder.Append(c);
            }
            return builder.ToString();
        }

        public virtual String ReadString(int charCount)
        {
            var bytes = this.m_reader.ReadBytes(charCount);
            string str = Encoding.ASCII.GetString(bytes);
            if (str.Length > 0 && str[str.Length - 1] == '\0')
            {
                return str.Substring(0, str.Length - 1);
            }
            return str;
        }

        public virtual int Read(char[] buffer, int index, int count) =>
          this.m_reader.Read(buffer, index, count);

        public virtual char[] ReadChars(int count) =>
          this.m_reader.ReadChars(count);

        public virtual int Read(byte[] buffer, int index, int count) =>
          this.m_reader.Read(buffer, index, count);

        public virtual byte[] ReadBytes(int count) =>
          this.m_reader.ReadBytes(count);

        public virtual int Skip(int count)
        {
            int i = 0;
            for (; i < count && !EndOfStream; i++)
            {
                this.m_reader.ReadByte();
            }
            return i;
        }

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            if (!this.m_reader.BaseStream.CanSeek) return 0;
            return this.m_reader.BaseStream.Seek(offset, origin);
        }

        public virtual void CopyTo(Stream destination, int bufferSize)
        {
            this.m_reader.BaseStream.CopyTo(destination, bufferSize);
        }

        public virtual void CopyTo(Stream destination)
        {
            this.m_reader.BaseStream.CopyTo(destination);
        }
    }
}
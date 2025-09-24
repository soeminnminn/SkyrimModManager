using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using IonKiwi.lz4;

namespace BsaBrowser.Archive
{
    internal static class Extensions
    {
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            if (!reader.BaseStream.CanRead) return string.Empty;

            StringBuilder builder = new StringBuilder();
            char endChar = '\0';
            char c = endChar;
            while ((c = reader.ReadChar()) != endChar)
            {
                builder.Append(c);
            }
            return builder.ToString();
        }

        public static string ReadString(this BinaryReader reader, int charCount)
        {
            if (!reader.BaseStream.CanRead) return string.Empty;

            string str = new string(reader.ReadChars(charCount));
            if (str.Length > 0 && str[str.Length - 1] == '\0')
            {
                return str.Substring(0, str.Length - 1);
            }
            return str;
        }

        public static ulong WriteInflateDecompress(this Stream stream, byte[] values)
        {
            int count;
            ulong written = 0;
            byte[] buffer = new byte[4096 * 10];

            var inflater = new Inflater();
            inflater.SetInput(values, 0, values.Length);

            while ((count = inflater.Inflate(buffer)) > 0)
            {
                stream.Write(buffer, 0, count);
                written += (ulong)count;
            }

            return written;
        }

        public static ulong WriteInflateDecompress(this BinaryWriter stream, byte[] values)
        {
            int count;
            ulong written = 0;
            byte[] buffer = new byte[4096 * 10];

            var inflater = new Inflater();
            inflater.SetInput(values, 0, values.Length);

            while ((count = inflater.Inflate(buffer)) > 0)
            {
                stream.Write(buffer, 0, count);
                written += (ulong)count;
            }

            return written;
        }

        public static ulong WriteLZ4Decompress(this Stream stream, byte[] values)
        {
            int count;
            ulong written = 0;
            byte[] buffer = new byte[4096 * 10];

            using (var ms = new MemoryStream(values, false))
            using (var lz4Stream = LZ4Stream.CreateDecompressor(ms, LZ4StreamMode.Read))
            {
                while ((count = lz4Stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, count);
                    written += (ulong)count;
                }
            }

            return written;
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.IO.Compression;

namespace ModManager.GameModules
{
    public static class Extensions
    {
        public static void CopyStream(this Stream src, Stream dst)
        {
            int num;
            byte[] buffer = new byte[0x400];
            do
            {
                num = src.Read(buffer, 0, buffer.Length);
                if (num > 0)
                {
                    dst.Write(buffer, 0, num);
                }
            }
            while (num != 0);
            dst.Seek(0L, SeekOrigin.Begin);
        }

        public static void CopyStream(this Stream src, Stream dst, int count)
        {
            byte[] buffer = new byte[0x400];
            do
            {
                int num = src.Read(buffer, 0, Math.Min(buffer.Length, count));
                if (num > 0)
                {
                    dst.Write(buffer, 0, num);
                    count -= num;
                }
                else
                {
                    count = 0;
                }
            }
            while (count > 0);
            dst.Seek(0L, SeekOrigin.Begin);
        }

        public static Stream Decompress(this Stream s, int size)
        {
            MemoryStream stream3 = new MemoryStream();
            using (MemoryStream dst = new MemoryStream())
            {
                s.CopyStream(dst, size);
                dst.Seek(0L, SeekOrigin.Begin);

                using (DeflateStream src = new DeflateStream(dst, CompressionMode.Decompress))
                {
                    src.CopyStream(stream3, (int)src.Length);
                    stream3.Seek(0L, SeekOrigin.Begin);

                    src.Close();
                }
                dst.Close();
            }
            return stream3;
        }
    }
}
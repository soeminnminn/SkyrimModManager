using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;

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
    }

    public class ArchiveFile : IDisposable, IEnumerable, IEnumerable<ArchiveFile.FileEntry>
    {
        #region Constants
        public const uint MW_HEADER_MAGIC = 0x00000100; //!< Magic for Morrowind BSA
        public const uint OB_HEADER_MAGIC = 0x00415342; //!< Magic for Oblivion BSA, the literal string "BSA\0".
        public const uint BA2_HEADER_MAGIC = 0x58445442; //!< Magic for BA2, the literal string "BTDX"

        public const uint OB_HEADER_VERSION = 0x67; //!< Version number of an Oblivion BSA
        public const uint F3_HEADER_VERSION = 0x68; //!< Version number of a Fallout 3 BSA
        public const uint SSE_HEADER_VERSION = 0x69; //!< Version number of a Skyrim Special Edition BSA

        public const uint OB_ARCHIVE_COMPRESSFILES = 0x0004;
        public const uint F3_ARCHIVE_PREFIXFULLFILENAMES = 0x0100;

        public const uint BA2_COMPRESS_GNRL = 0x4C524E47;
        public const uint BA2_COMPRESS_DX10 = 0x30315844;

        private const uint SSE_BSA_FOLDER_INFO_SIZE = 24;
        private const uint OB_BSA_FOLDER_INFO_SIZE = 20;
        private const uint BSA_FILE_INFO_SIZE = 16;
        #endregion

        #region Variables
        private string mFilePath = null;
        private BinaryReader mReader = null;

        private uint mMagic = 0;
        private uint mVersion = 0;
        private uint mRecordOffset = 0;
        private uint mArchiveFlags = 0;
        private uint mFolderCount = 0;
        private uint mFileCount = 0;
        private uint mFolderNameLength = 0;
        private uint mFileNameLength = 0;
        private uint mFileFlags = 0;

        private bool mCompressed = false;
        private bool mContainsFileNameBlobs = false;

        private List<FileEntry> mFiles = null;
        private ArchiveNode mNode = null;
        #endregion

        #region Constructor
        public ArchiveFile(string filePath)
        {
            this.mFilePath = filePath;
        }
        #endregion

        #region Methods
        internal static string GetDirectoryName(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Regex regex = new Regex(@"^(.*)\\[^\\]+$");
                Match match = regex.Match(filePath);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }

        internal static string GetFileName(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Regex regex = new Regex(@"^.*\\([^\\]+)$");
                Match match = regex.Match(filePath);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }

        internal static string GetFileNameExtension(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                Regex regex = new Regex(@"^.*\.([^\.]+)$");
                Match match = regex.Match(filePath);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return string.Empty;
        }

        private void CopyStream(Stream src, Stream dst)
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

        #region Load
        private void LoadOldBSA()
        {
            //Might be a fallout 2 dat
            this.mReader.BaseStream.Position = this.mReader.BaseStream.Length - 8;

            uint treeSize = this.mReader.ReadUInt32();
            uint dataSize = this.mReader.ReadUInt32();

            if (dataSize != this.mReader.BaseStream.Length)
            {
                this.mReader.Close();
                throw new ArgumentException("File is not a valid bsa archive.", "filePath");
            }

            this.mRecordOffset = dataSize - treeSize - 8;
            this.mReader.BaseStream.Position = this.mRecordOffset;
            this.mFileCount = this.mReader.ReadUInt32();

            for (int i = 0; i < this.mFileCount; i++)
            {
                int fileNameLen = this.mReader.ReadInt32();
                string fullPath = this.mReader.ReadString(fileNameLen);

                FileEntry file = new FileEntry();
                byte compressed = this.mReader.ReadByte();
                file.compressed = (compressed != 0);
                file.realSize = this.mReader.ReadUInt32();
                file.compressSize = this.mReader.ReadUInt32();
                file.size = file.compressSize;
                file.offset = this.mReader.ReadUInt32();

                file.fullPath = fullPath;
                file.folderPath = GetDirectoryName(fullPath);
                file.name = GetFileName(fullPath);
                file.extension = GetFileNameExtension(fullPath);

                this.mFiles.Add(file);
            }
        }

        private void LoadMorrowind()
        {
            this.mRecordOffset = this.mReader.ReadUInt32();
            this.mFileCount = this.mReader.ReadUInt32();

            uint dataOffset = 12 + this.mRecordOffset + this.mFileCount * 8;
            for (int i = 0; i < this.mFileCount; i++)
            {
                FileEntry file = new FileEntry();
                file.size = this.mReader.ReadUInt32();
                file.offset = this.mReader.ReadUInt32() + dataOffset;
                this.mFiles.Add(file);
            }
            for (int i = 0; i < this.mFileCount; i++)
            {
                this.mFiles[i].nameOffset = this.mReader.ReadUInt32();
            }
            for (int i = 0; i < this.mFileCount; i++)
            {
                string fullPath = this.mReader.ReadNullTerminatedString();
                this.mFiles[i].fullPath = fullPath;
                this.mFiles[i].folderPath = GetDirectoryName(fullPath);
                this.mFiles[i].name = GetFileName(fullPath);
                this.mFiles[i].extension = GetFileNameExtension(fullPath);
            }
        }

        private void LoadOblivionPlus()
        {
            this.mVersion = this.mReader.ReadUInt32();
            this.mRecordOffset = this.mReader.ReadUInt32();
            this.mArchiveFlags = this.mReader.ReadUInt32();
            this.mFolderCount = this.mReader.ReadUInt32();
            this.mFileCount = this.mReader.ReadUInt32();
            this.mFolderNameLength = this.mReader.ReadUInt32();
            this.mFileNameLength = this.mReader.ReadUInt32();
            this.mFileFlags = this.mReader.ReadUInt32();

            this.mCompressed = (this.mArchiveFlags & OB_ARCHIVE_COMPRESSFILES) > 0;
            this.mContainsFileNameBlobs = (this.mArchiveFlags & F3_ARCHIVE_PREFIXFULLFILENAMES) > 0;

            List<FolderEntry> folderInfos = new List<FolderEntry>();
            for (int i = 0; i < this.mFolderCount; i++)
            {
                FolderEntry folder = new FolderEntry();
                folder.folderHash = this.mReader.ReadUInt64();
                folder.fileCount = this.mReader.ReadUInt32();
                if (this.mVersion == SSE_HEADER_VERSION)
                {
                    folder.unk1 = this.mReader.ReadUInt32();
                    folder.fileOffset = this.mReader.ReadUInt64();
                }
                else
                {
                    folder.fileOffset = this.mReader.ReadUInt32();
                }
                
                folderInfos.Add(folder);
            }

            for (int i = 0; i < this.mFolderCount; i++)
            {
                int len = this.mReader.ReadByte();
                string folderPath = this.mReader.ReadString(len);

                for (int j = 0; j < folderInfos[i].fileCount; j++)
                {
                    FileEntry file = new FileEntry();
                    file.hash = this.mReader.ReadUInt64();
                    file.size = this.mReader.ReadUInt32();
                    file.offset = this.mReader.ReadUInt32();
                    file.folderPath = folderPath;
                    bool compressed = this.mCompressed;
                    if (this.mVersion != SSE_HEADER_VERSION && (file.size & (1 << 30)) != 0)
                    {
                        compressed = !compressed;
                        file.size ^= 1 << 30;
                    }
                    file.compressed = compressed;

                    this.mFiles.Add(file);
                }
            }

            for (int i = 0; i < this.mFileCount; i++)
            {
                this.mFiles[i].name = this.mReader.ReadNullTerminatedString();
                this.mFiles[i].fullPath = string.Format("{0}\\{1}", this.mFiles[i].folderPath, this.mFiles[i].name);
                this.mFiles[i].extension = GetFileNameExtension(this.mFiles[i].name);
            }
        }

        private void LoadBa2()
        {
            this.mVersion = this.mReader.ReadUInt32();
            this.mArchiveFlags = this.mReader.ReadUInt32();
            this.mFileCount = this.mReader.ReadUInt32();
            ulong nameTableOffset = this.mReader.ReadUInt64();

            for (int i = 0; i < this.mFileCount; i++)
            {
                if (this.mArchiveFlags == BA2_COMPRESS_GNRL)
                {
                    FileEntry file = new FileEntry();

                    file.nameHash = this.mReader.ReadUInt32();
                    file.extension = this.mReader.ReadString(4);
                    file.dirHash = this.mReader.ReadUInt32();
                    file.flags = this.mReader.ReadUInt32();
                    file.offset = this.mReader.ReadUInt64();
                    file.size = this.mReader.ReadUInt32();
                    file.realSize = this.mReader.ReadUInt32();
                    file.align = this.mReader.ReadUInt32();

                    bool compressed = (file.realSize != 0);

                    file.compressed = compressed;
                    file.compressSize = compressed ? file.size : 0;
                    file.hash = file.dirHash & file.nameHash;
                    file.size = compressed ? file.realSize : file.size;
                    this.mFiles.Add(file);
                }
                else if (this.mArchiveFlags == BA2_COMPRESS_DX10)
                {
                    TextureEntry texture = new TextureEntry();

                    texture.nameHash = this.mReader.ReadUInt32();
                    texture.extension = this.mReader.ReadString(4);
                    texture.dirHash = this.mReader.ReadUInt32();
                    texture.unk8 = this.mReader.ReadByte();
                    texture.numChunks = this.mReader.ReadByte();
                    texture.chunkHdrLen = this.mReader.ReadUInt16();
                    texture.height = this.mReader.ReadUInt16();
                    texture.width = this.mReader.ReadUInt16();
                    texture.numMips = this.mReader.ReadByte();
                    texture.format = this.mReader.ReadByte();
                    texture.unk16 = this.mReader.ReadUInt16();

                    for (int j = 0; j < texture.numChunks; j++)
                    {
                        TextureChunk chunk = new TextureChunk();
                        chunk.offset = this.mReader.ReadUInt64();
                        chunk.packSize = this.mReader.ReadUInt32();
                        chunk.fullSize = this.mReader.ReadUInt32();
                        chunk.startMip = this.mReader.ReadUInt16();
                        chunk.endMip = this.mReader.ReadUInt16();
                        chunk.align = this.mReader.ReadUInt32();
                        texture.Chunks.Add(chunk);
                    }

                    this.mFiles.Add(texture);
                }
                else
                {
                    FileEntry file = new FileEntry();
                    this.mFiles.Add(file);
                }
            }

            // Seek to name table
            this.mReader.BaseStream.Seek((long)nameTableOffset, SeekOrigin.Begin);

            // Assign full names to each file
            for (int i = 0; i < this.mFileCount; i++)
            {
                short length = this.mReader.ReadInt16();
                string fullPath = this.mReader.ReadString(length);
                this.mFiles[i].fullPath = fullPath;
                this.mFiles[i].folderPath = GetDirectoryName(fullPath);
                this.mFiles[i].name = GetFileName(fullPath);
            }
        }
        
        public bool Load()
        {
            if (string.IsNullOrEmpty(this.mFilePath)) return false;
            FileInfo file = new FileInfo(this.mFilePath);
            if (!file.Exists) return false;

            this.mFiles = new List<FileEntry>();

            this.mReader = new BinaryReader(file.OpenRead(), Encoding.Default);
            this.mMagic = this.mReader.ReadUInt32();

            if (this.mMagic == BA2_HEADER_MAGIC)
            {
                this.LoadBa2();
            }
            else if (this.mMagic == MW_HEADER_MAGIC)
            {
                this.LoadMorrowind();
            }
            else if (this.mMagic == OB_HEADER_MAGIC)
            {
                this.LoadOblivionPlus();
            }
            else
            {
                this.LoadOldBSA();
            }
            System.Diagnostics.Trace.WriteLine(string.Format("File Loaded : {0} files", this.mFiles.Count));

            this.mNode = new ArchiveNode(file.FullName, file.Name, this.mFiles);
            this.mNode.Sort();

            return true;
        }
        #endregion

        #region Extract
        private uint ExtractBSA(FileEntry entry, Stream stream)
        {
            this.mReader.BaseStream.Position = (long)entry.offset;
            if (this.mContainsFileNameBlobs)
            {
                this.mReader.BaseStream.Position += this.mReader.ReadByte() + 1;
            }

            uint filesize = entry.size;
            if (entry.compressed)
            {
                byte[] bytes = new byte[filesize];
                this.mReader.Read(bytes, 0, (int)filesize);
                stream.Write(bytes, 0, (int)filesize);
            }
            else
            {
                byte[] uncompressed;
                if (entry.realSize == 0)
                {
                    filesize = this.mReader.ReadUInt32();
                    uncompressed = new byte[filesize];
                }
                else
                {
                    filesize = entry.realSize;
                    uncompressed = new byte[entry.realSize];
                }

                byte[] compressed = new byte[entry.size - 4];
                this.mReader.Read(compressed, 0, (int)(entry.size - 4));

                Inflater inflater = new Inflater();
                inflater.SetInput(compressed);
                inflater.Inflate(uncompressed);
                stream.Write(uncompressed, 0, uncompressed.Length);
            }
            return filesize;
        }

        private uint ExtractSSEBSA(FileEntry entry, Stream stream)
        {
            this.mReader.BaseStream.Seek((long)entry.offset, SeekOrigin.Begin);
            ulong filesz = entry.size & 0x3fffffff;
            if (this.mContainsFileNameBlobs)
            {
                int len = this.mReader.ReadByte();
                filesz -= (ulong)len + 1;
                this.mReader.BaseStream.Seek((long)(entry.offset + (ulong)1 + (ulong)len), SeekOrigin.Begin);
            }

            uint filesize = (uint)filesz;
            if (entry.size > 0 && entry.compressed)
            {
                filesize = this.mReader.ReadUInt32();
                filesz -= 4;
            }

            byte[] content = this.mReader.ReadBytes((int)filesz);
            if (!entry.compressed)
            {
                stream.Write(content, 0, content.Length);
            }
            else
            {
                using (var ms = new MemoryStream(content, false))
                using (var inputStream = new InflaterInputStream(ms))
                {
                    inputStream.CopyTo(stream);
                }
            }
            return filesize;
        }

        private uint ExtractBA2(FileEntry entry, Stream stream)
        {
            this.mReader.BaseStream.Seek((long)entry.offset, SeekOrigin.Begin);

            byte[] bytes = new byte[entry.size];

            uint filesize = entry.size;
            if (entry.size == 0)
            {
                filesize = entry.realSize;
                bytes = new byte[entry.realSize];
                this.mReader.Read(bytes, 0, (int)entry.realSize);
                stream.Write(bytes, 0, (int)entry.realSize);
                stream.Seek(0, SeekOrigin.Begin);
                return filesize;
            }

            this.mReader.Read(bytes, 0, (int)entry.size);

            if (!entry.compressed)
            {
                stream.Write(bytes, 0, (int)entry.size);
            }
            else
            {
                filesize = entry.realSize;
                byte[] uncompressed = new byte[entry.realSize];

                Inflater inflater = new Inflater();
                inflater.SetInput(bytes);
                inflater.Inflate(uncompressed);

                stream.Write(uncompressed, 0, uncompressed.Length);
            }
            return filesize;
        }

        private void ExtractBA2Texture(TextureEntry entry, Stream stream)
        {
            DDS_HEADER ddsHeader = new DDS_HEADER();
            ddsHeader.dwSize = ddsHeader.GetSize();
            ddsHeader.dwHeaderFlags = DDS.DDS_HEADER_FLAGS_TEXTURE | DDS.DDS_HEADER_FLAGS_LINEARSIZE | DDS.DDS_HEADER_FLAGS_MIPMAP;
            ddsHeader.dwHeight = entry.height;
            ddsHeader.dwWidth = entry.width;
            ddsHeader.dwMipMapCount = entry.numMips;
            ddsHeader.PixelFormat.dwSize = ddsHeader.PixelFormat.GetSize();
            ddsHeader.dwSurfaceFlags = DDS.DDS_SURFACE_FLAGS_TEXTURE | DDS.DDS_SURFACE_FLAGS_MIPMAP;

            switch ((DXGI_FORMAT)entry.format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_FOURCC;
                    ddsHeader.PixelFormat.dwFourCC = DDS.MAKEFOURCC('D', 'X', 'T', '1');
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height / 2); // 4bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_FOURCC;
                    ddsHeader.PixelFormat.dwFourCC = DDS.MAKEFOURCC('D', 'X', 'T', '3');
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height); // 8bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_FOURCC;
                    ddsHeader.PixelFormat.dwFourCC = DDS.MAKEFOURCC('D', 'X', 'T', '5');
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height); // 8bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_FOURCC;
                    ddsHeader.PixelFormat.dwFourCC = DDS.MAKEFOURCC('D', 'X', 'T', '5');
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height); // 8bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    // totally wrong but not worth writing out the DX10 header
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_FOURCC;
                    ddsHeader.PixelFormat.dwFourCC = DDS.MAKEFOURCC('B', 'C', '7', '\0');
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height); // 8bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_RGBA;
                    ddsHeader.PixelFormat.dwRGBBitCount = 32;
                    ddsHeader.PixelFormat.dwRBitMask = 0x00FF0000;
                    ddsHeader.PixelFormat.dwGBitMask = 0x0000FF00;
                    ddsHeader.PixelFormat.dwBBitMask = 0x000000FF;
                    ddsHeader.PixelFormat.dwABitMask = 0xFF000000;
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height * 4); // 32bpp
                    break;
                case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                    ddsHeader.PixelFormat.dwFlags = DDS.DDS_RGB;
                    ddsHeader.PixelFormat.dwRGBBitCount = 8;
                    ddsHeader.PixelFormat.dwRBitMask = 0xFF;
                    ddsHeader.dwPitchOrLinearSize = (uint)(entry.width * entry.height); // 8bpp
                    break;
                default:
                    return;
            }

            var temp = new MemoryStream();

            using (var bw = new BinaryWriter(temp))
            {
                bw.Write((uint)DDS.DDS_MAGIC);
                ddsHeader.Write(bw);

                for (int i = 0; i < entry.numChunks; i++)
                {
                    byte[] compressed = new byte[entry.Chunks[i].packSize];
                    byte[] full = new byte[entry.Chunks[i].fullSize];
                    bool isCompressed = entry.Chunks[i].packSize != 0;

                    this.mReader.BaseStream.Seek((long)entry.Chunks[i].offset, SeekOrigin.Begin);

                    if (!entry.compressed)
                    {
                        this.mReader.Read(full, 0, full.Length);
                    }
                    else
                    {
                        this.mReader.Read(compressed, 0, compressed.Length);
                        // Uncompress
                        Inflater inflater = new Inflater();
                        inflater.SetInput(compressed);
                        inflater.Inflate(full);
                    }

                    bw.Write(full);
                }

                byte[] bytes = temp.GetBuffer();
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public void ExtractTo(FileEntry entry, Stream stream)
        {
            if (this.mMagic == BA2_HEADER_MAGIC)
            {
                if (entry is TextureEntry)
                {
                    TextureEntry textureEntry = (TextureEntry)entry;
                    this.ExtractBA2Texture(textureEntry, stream);
                }
                else
                {
                    this.ExtractBA2(entry, stream);
                }
            }
            else if (this.mVersion == SSE_HEADER_VERSION)
            {
                this.ExtractSSEBSA(entry, stream);
            }
            else
            {
                this.ExtractBSA(entry, stream);
            }
        }

        public void ExtractTo(FileEntry entry, string destPath)
        {
            MemoryStream os = new MemoryStream();
            if (!Directory.Exists(Path.GetDirectoryName(destPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            }
            ExtractTo(entry, os);

            os.Seek(0L, SeekOrigin.Begin);
            using (FileStream stream = File.OpenWrite(destPath))
            {
                CopyStream(os, stream);
                stream.Close();
            }
        }

        public int ExtractAll(string destPath)
        {
            if (this.mFiles == null) return 0;
            if (this.mFiles.Count == 0) return 0;

            destPath = destPath.TrimEnd('\\');

            int count = 0;
            foreach(var record in this.mFiles)
            {
                string recordPath = record.fullPath;
                string destFilePath = destPath + "\\" + recordPath.TrimStart('\\');
                ExtractTo(record, destFilePath);
                count++;
            }
            return count;
        }
        #endregion

        public void Dispose()
        {
            if (this.mReader != null)
            {
                this.mReader.Close();
                this.mReader = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.mFiles.GetEnumerator();
        }

        public IEnumerator<FileEntry> GetEnumerator()
        {
            return this.mFiles.GetEnumerator();
        }
        #endregion

        #region Properties
        public string FilePath
        {
            get
            {
                return this.mFilePath;
            }
        }

        public FileEntry this[int index]
        {
            get
            {
                if (this.mFiles != null && index > -1 && index < this.mFiles.Count)
                {
                    return this.mFiles[index];
                }
                return null;
            }
        }

        public ArchiveNode Node
        {
            get
            {
                return this.mNode;
            }
        }
        #endregion

        #region Nested Types
        private class FolderEntry
        {
            #region Variables
            public ulong folderHash = 0;
            public uint fileCount = 0;
            public uint unk1 = 0;
            public ulong fileOffset = 0;
            #endregion
        }

        public class FileEntry
        {
            #region Variables
            public ulong hash = 0;
            public uint size = 0;
            public ulong offset = 0;
            public uint compressSize = 0;
            public bool compressed = false;
            public uint nameOffset = 0;
            public string name = string.Empty;
            public string folderPath = string.Empty;
            
            // BA2
            public uint nameHash = 0;
            public string extension = string.Empty;
            public uint dirHash = 0;
            public uint flags = 0;
            public uint realSize = 0;
            public uint align = 0;
            public string fullPath = string.Empty;
            #endregion

            #region Methods
            public override string ToString()
            {
                if (string.IsNullOrEmpty(this.folderPath))
                {
                    return this.name;
                }
                return string.Format("{0}\\{1}", this.folderPath, this.name);
            }
            #endregion
        }

        public class TextureEntry : FileEntry
        {
            #region Variables
            public byte unk8 = 0;
            public byte numChunks = 0;
            public ushort chunkHdrLen = 0;
            public ushort height = 0;
            public ushort width = 0;
            public byte numMips = 0;
            public byte format = 0;
            public ushort unk16 = 0;

            public List<TextureChunk> Chunks = new List<TextureChunk>();
            #endregion
        }

        public class TextureChunk
        {
            #region Variables
            public ulong offset = 0;
            public uint packSize = 0;
            public uint fullSize = 0;
            public ushort startMip = 0;
            public ushort endMip = 0;
            public uint align = 0;
            #endregion
        }

        private class DDS
        {
            public const int DDS_MAGIC = 0x20534444; // "DDS "

            public static uint MAKEFOURCC(char ch0, char ch1, char ch2, char ch3)
            {
                // This is alien to me...
                return ((uint)(byte)(ch0) | ((uint)(byte)(ch1) << 8) | ((uint)(byte)(ch2) << 16 | ((uint)(byte)(ch3) << 24)));
            }

            public const int DDS_FOURCC = 0x00000004; // DDPF_FOURCC
            public const int DDS_RGB = 0x00000040; // DDPF_RGB
            public const int DDS_RGBA = 0x00000041; // DDPF_RGB | DDPF_ALPHAPIXELS

            public const int DDS_HEADER_FLAGS_TEXTURE = 0x00001007; // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT
            public const int DDS_HEADER_FLAGS_MIPMAP = 0x00020000; // DDSD_MIPMAPCOUNT
            public const int DDS_HEADER_FLAGS_LINEARSIZE = 0x00080000; // DDSD_LINEARSIZE

            public const int DDS_SURFACE_FLAGS_TEXTURE = 0x00001000; // DDSCAPS_TEXTURE
            public const int DDS_SURFACE_FLAGS_MIPMAP = 0x00400008; // DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
        }

        private enum DXGI_FORMAT
        {
            DXGI_FORMAT_R8_UNORM = 61,
            DXGI_FORMAT_BC1_UNORM = 71,
            DXGI_FORMAT_BC2_UNORM = 74,
            DXGI_FORMAT_BC3_UNORM = 77,
            DXGI_FORMAT_BC5_UNORM = 83,
            DXGI_FORMAT_B8G8R8A8_UNORM = 87,
            DXGI_FORMAT_BC7_UNORM = 98
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DDS_HEADER
        {
            public uint dwSize;
            public uint dwHeaderFlags;
            public uint dwHeight;
            public uint dwWidth;
            public uint dwPitchOrLinearSize;
            public uint dwDepth; // only if DDS_HEADER_FLAGS_VOLUME is set in dwHeaderFlags
            public uint dwMipMapCount;
            public uint dwReserved1; // [11]
            public DDS_PIXELFORMAT PixelFormat; // ddspf
            public uint dwSurfaceFlags;
            public uint dwCubemapFlags;
            public uint dwReserved2; // [3]

            public uint GetSize()
            {
                // 9 uint + DDS_PIXELFORMAT uints + 2 uint arrays with 14 uints total
                // each uint 4 bytes each
                return (9 * 4) + PixelFormat.GetSize() + (14 * 4);
            }

            public void Write(System.IO.BinaryWriter bw)
            {
                bw.Write(dwSize);
                bw.Write(dwHeaderFlags);
                bw.Write(dwHeight);
                bw.Write(dwWidth);
                bw.Write(dwPitchOrLinearSize);
                bw.Write(dwDepth);
                bw.Write(dwMipMapCount);

                // Just write it multiple times, since it's never assigned a value anyway
                for (int i = 0; i < 11; i++)
                    bw.Write(dwReserved1);

                // DDS_PIXELFORMAT
                bw.Write(PixelFormat.dwSize);
                bw.Write(PixelFormat.dwFlags);
                bw.Write(PixelFormat.dwFourCC);
                bw.Write(PixelFormat.dwRGBBitCount);
                bw.Write(PixelFormat.dwRBitMask);
                bw.Write(PixelFormat.dwGBitMask);
                bw.Write(PixelFormat.dwBBitMask);
                bw.Write(PixelFormat.dwABitMask);

                bw.Write(dwSurfaceFlags);
                bw.Write(dwCubemapFlags);

                // Just write it multiple times, since it's never assigned a value anyway
                for (int i = 0; i < 3; i++)
                    bw.Write(dwReserved2);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DDS_PIXELFORMAT
        {
            public uint dwSize;
            public uint dwFlags;
            public uint dwFourCC;
            public uint dwRGBBitCount;
            public uint dwRBitMask;
            public uint dwGBitMask;
            public uint dwBBitMask;
            public uint dwABitMask;

            public DDS_PIXELFORMAT(uint size, uint flags, uint fourCC, uint rgbBitCount, uint rBitMask, uint gBitMask, uint bBitMask, uint aBitMask)
            {
                dwSize = size;
                dwFlags = flags;
                dwFourCC = fourCC;
                dwRGBBitCount = rgbBitCount;
                dwRBitMask = rBitMask;
                dwGBitMask = gBitMask;
                dwBBitMask = bBitMask;
                dwABitMask = aBitMask;
            }

            public uint GetSize()
            {
                // 8 uints, each 4 bytes each
                return 8 * 4;
            }
        }
        #endregion
    }

    public partial class ArchiveNode : IEnumerable, IEnumerable<ArchiveNode>
    {
        #region Variables
        private string mName = string.Empty;
        private string mPath = string.Empty;
        private ArchiveFile.FileEntry mFileEntry = null;
        private Dictionary<string, ArchiveNode> mChilds = null;
        #endregion

        #region Constructor
        public ArchiveNode()
        {
            this.mChilds = new Dictionary<string, ArchiveNode>();
        }

        public ArchiveNode(string path, string name)
            : this()
        {
            this.mPath = path;
            this.mName = name;
        }

        public ArchiveNode(string path, string name, List<ArchiveFile.FileEntry> files)
            : this(path, name)
        {
            for (int i = 0; i < files.Count; i++)
            {
                this.AddFile(files[i]);
            }
        }
        #endregion

        #region Methods
        private void AddFile(ArchiveFile.FileEntry file)
        {
            string fullPath = file.fullPath;
            if (!string.IsNullOrEmpty(fullPath))
            {
                string[] pArr = fullPath.Split('\\');
                string path = string.Empty;
                ArchiveNode node = this;
                for (int i = 0; i < pArr.Length; i++)
                {
                    string p = pArr[i];
                    if (!string.IsNullOrEmpty(path))
                    {
                        path += "\\";
                    }
                    path += p;

                    ArchiveNode current;
                    if (node.mChilds.ContainsKey(p))
                    {
                        current = node.mChilds[p];
                    }
                    else
                    {
                        current = new ArchiveNode(path, p);
                        if (i == pArr.Length - 1)
                        {
                            current.mFileEntry = file;
                        }
                        node.mChilds.Add(p, current);
                    }
                    node = current;
                }
            }
        }

        public void Sort()
        {
            ArchiveNode[] nodes = new ArchiveNode[this.mChilds.Count];
            this.mChilds.Values.CopyTo(nodes, 0);
            Array.Sort(nodes, (ArchiveNode node1, ArchiveNode node2) =>
            {
                if (node1.IsFolder && !node2.IsFolder)
                {
                    return -1;
                }
                else if (!node1.IsFolder && node2.IsFolder)
                {
                    return 1;
                }
                return node1.Name.CompareTo(node2.Name);
            });

            this.mChilds = new Dictionary<string, ArchiveNode>();
            foreach(ArchiveNode node in nodes)
            {
                node.Sort();
                this.mChilds.Add(node.Name, node);
            }
        }

        public override string ToString()
        {
            return this.mPath;
        }

        public IEnumerator<ArchiveNode> GetEnumerator()
        {
            foreach (var node in mChilds.Values.ToArray())
            {
                yield return node;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var node in mChilds.Values.ToArray())
            {
                yield return node;
            }
        }
        #endregion

        #region Properties
        public string Name
        {
            get => mName;
        }

        public string Path
        {
            get => mPath;
        }

        public string DirPath
        {
            get
            {
                return ArchiveFile.GetDirectoryName(mPath);
            }
        }

        public bool IsFolder
        {
            get => mFileEntry == null;
        }

        public long Size
        {
            get
            {
                if (mFileEntry != null)
                {
                    if (mFileEntry.realSize > 0) 
                        return mFileEntry.realSize;
                    return mFileEntry.size;
                }
                return mChilds.Count;
            }
        }

        public long CompressedSize
        {
            get
            {
                if (mFileEntry != null)
                    return mFileEntry.compressSize;

                return 0;
            }
        }

        public long Offset
        {
            get
            {
                if (mFileEntry != null)
                    return (long)mFileEntry.offset;

                return 0;
            }
        }

        public ArchiveFile.FileEntry Entry
        {
            get => mFileEntry;
        }

        public ArchiveNode this[int index]
        {
            get
            {
                var values = mChilds.Values.ToArray();
                if (index > -1 && index < values.Length)
                {
                    return values[index];
                }
                return null;
            }
        }

        public IEnumerable<ArchiveNode> Folders
        {
            get => mChilds.Values.Where(x => x.IsFolder).AsEnumerable();
        }

        public IEnumerable<ArchiveNode> Files
        {
            get => mChilds.Values.Where(x => !x.IsFolder).AsEnumerable();
        }
        #endregion
    }
}

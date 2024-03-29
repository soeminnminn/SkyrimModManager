﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BsaBrowser.Commons
{
    // From https://stackoverflow.com/a/59129804
    public static class SystemIcons
    {
        private static Dictionary<string, Icon> _cache = new Dictionary<string, Icon>();

        public static Icon Files => GetCachedIcon(nameof(Files), SHSTOCKICONID.SIID_DRIVEFIXED, SHGFI_SMALLICON);
        public static Icon FilesNoAssoc => GetCachedIcon(nameof(FilesNoAssoc), SHSTOCKICONID.SIID_DOCNOASSOC, SHGFI_SMALLICON);
        public static Icon FolderLarge => GetCachedIcon(nameof(FolderLarge), SHSTOCKICONID.SIID_FOLDER, SHGFI_LARGEICON);
        public static Icon FolderSmall => GetCachedIcon(nameof(FolderSmall), SHSTOCKICONID.SIID_FOLDER, SHGFI_SMALLICON);
        public static Icon FolderLargeOpen => GetCachedIcon(nameof(FolderLargeOpen), SHSTOCKICONID.SIID_FOLDEROPEN, SHGFI_LARGEICON);
        public static Icon FolderSmallOpen => GetCachedIcon(nameof(FolderSmallOpen), SHSTOCKICONID.SIID_FOLDEROPEN, SHGFI_SMALLICON);

        public static Icon GetFileIcon(string filepath, bool smallIcon = true)
        {
            string cacheKey = GetFileNameExtension(filepath).ToLowerInvariant() + (smallIcon ? string.Empty : "|large");
            if (_cache.TryGetValue(cacheKey, out Icon cachedIcon))
            {
                return cachedIcon;
            }

            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            if (smallIcon)
            {
                flags = flags | SHGFI_SMALLICON;
            }
            else
            {
                flags = flags | SHGFI_LARGEICON;
            }

            SHGetFileInfo(filepath, FILE_ATTRIBUTE_NORMAL, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            Icon icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone(); // Get a copy that doesn't use the original handle
            DestroyIcon(shfi.hIcon); // Clean up native icon to prevent resource leak

            _cache.Add(cacheKey, icon);

            return icon;
        }

        private static string GetFileNameExtension(string filePath)
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

        public static Icon GetFolderIcon(string folderPath, bool openFolder, bool smallIcon = true)
        {
            string cacheKey = openFolder ? "FolderSmallOpen" : "FolderSmall";
            if (System.IO.Directory.Exists(folderPath))
            {
                cacheKey = folderPath + (smallIcon ? string.Empty : "|large");
            }
            else if (smallIcon)
            {
                cacheKey = openFolder ? "FolderLargeOpen" : "FolderLarge";
            }
            if (_cache.TryGetValue(cacheKey, out Icon cachedIcon))
            {
                return cachedIcon;
            }


            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            
            if (openFolder)
            {
                flags = flags | SHGFI_OPENICON;
            }

            if (smallIcon)
            {
                flags = flags | SHGFI_SMALLICON;
            }
            else
            {
                flags = flags | SHGFI_LARGEICON;
            }

            SHGetFileInfo(folderPath, FILE_ATTRIBUTE_FOLDER, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            Icon icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone(); // Get a copy that doesn't use the original handle
            DestroyIcon(shfi.hIcon); // Clean up native icon to prevent resource leak

            _cache.Add(cacheKey, icon);

            return icon;
        }

        private static Icon GetCachedIcon(string name, SHSTOCKICONID type, uint size)
        {
            if (!_cache.ContainsKey(name))
                _cache.Add(name, GetStockIcon(type, size));
            return _cache[name];
        }

        private static Icon GetStockIcon(SHSTOCKICONID type, uint size)
        {
            var info = new SHSTOCKICONINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            SHGetStockIconInfo((uint)type, SHGFI_ICON | size, ref info);

            var icon = (Icon)Icon.FromHandle(info.hIcon).Clone(); // Get a copy that doesn't use the original handle
            DestroyIcon(info.hIcon); // Clean up native icon to prevent resource leak

            return icon;
        }

        public static ImageSource ToImageSource(this Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public const int NAMESIZE = 80;
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NAMESIZE)]
            public string szTypeName;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysIconIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [DllImport("Shell32.dll")]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll")]
        private static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref SHSTOCKICONINFO psii);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        private enum SHSTOCKICONID : uint
        {
            SIID_DOCNOASSOC = 0x0,
            SIID_FOLDER = 0x3,
            SIID_FOLDEROPEN = 0x4,
            SIID_DRIVEFIXED = 0x8
        }

        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_ATTRIBUTE_FOLDER = 0x10;

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x1;
        private const uint SHGFI_OPENICON = 2;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
    }
}

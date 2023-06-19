using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BsaBrowser.Archive
{
    public partial class ArchiveNode : INotifyPropertyChanged
    {
        private static readonly string[] sizeSuffixes = new string[] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public event PropertyChangedEventHandler PropertyChanged = null;

        private bool mIsExpanded = false;
        private bool mIsSelected = false;

        public ArchiveNode(ArchiveFile.FileEntry entry)
            : this()
        {
            mFileEntry = entry;
            if (entry != null)
            {
                mPath = entry.fullPath;
                mName = entry.name;
            }
        }

        public bool IsExpanded
        {
            get => mIsExpanded;
            set { SetProperty(ref mIsExpanded, value); }
        }

        public bool IsSelected
        {
            get => mIsSelected;
            set { SetProperty(ref mIsSelected, value); }
        }

        public string OffsetStr
        {
            get
            {
                if (IsFolder) return string.Empty;
                return $"0x{Offset:X8}";
            }
        }

        private static string FormatFileSize(long bytes, int decimalPlaces = 2)
        {
            var s = sizeSuffixes;
            var value = Math.Abs(bytes);

            if (value == 0)
                return "0 bytes";

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, s[mag]);
        }

        public string FileSizeStr
        {
            get
            {
                if (IsFolder) return string.Empty;
                return FormatFileSize(Size);
            }
        }

        public string CompressedSizeStr
        {
            get
            {
                if (IsFolder) return string.Empty;
                if (CompressedSize == 0) return FileSizeStr;
                return FormatFileSize(CompressedSize);
            }
        }

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action onChanged = null, Func<T, T, bool> validateValue = null)
        {
            //if value didn't change
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            //if value changed but didn't validate
            if (validateValue != null && !validateValue(backingStore, value))
                return false;

            backingStore = value;

            onChanged?.Invoke();

            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

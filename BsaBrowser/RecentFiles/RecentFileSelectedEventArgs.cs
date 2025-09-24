using System;

namespace BsaBrowser.RecentFiles
{
    public sealed class RecentFileSelectedEventArgs
    {
        internal RecentFileSelectedEventArgs(string fileName) => FileName = fileName;

        public string FileName { get; }
    }
}

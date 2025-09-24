using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BsaBrowser.Archive
{
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
            foreach (ArchiveNode node in nodes)
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

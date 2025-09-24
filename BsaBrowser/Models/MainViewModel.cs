using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using BsaBrowser.Archive;
using BsaBrowser.Commons;
using BsaBrowser.RecentFiles;
using BsaBrowser.Wildcard;

namespace BsaBrowser.Models
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        #region Variables
        private static readonly string[] fileProperties = { "Title", "FilePath", "IsFileOpened", "IsHasFiles" };
        private const string MainContent = "/Views/MainContent.xaml";
        private const string SearchContent = "/Views/SearchContent.xaml";

        private OpenFileDialog fileDialog = new()
        {
            Title = "Open BSA File",
            Filter = "BSA, BA2 File|*.BSA;*.BA2|All Files|*.*",
            CheckFileExists = true
        };
        private Controls.FolderPicker saveFolderDialog = new();

        private string mFileName = string.Empty;
        private bool mIsProcessing = false;
        
        private ArchiveFile mArchiveFile = null;
        private ArchiveNode mNode = null;
        private ArchiveNode mTreeSelected = null;
        private IList<ArchiveNode> mSelectedNodes = null;
        private ObservableCollection<ArchiveNode> mFoundNodes = new();

        private string mCopyTempPath = string.Empty;
        private IDataObject mCopyObject = null;

        private IRecentFilesMenu recentFilesMenu;
        private string mContent = MainContent;
        #endregion

        #region Properties
        public string Title
        {
            get  
            {
                if (!string.IsNullOrEmpty(mFileName))
                {
                    return string.Format("BSA Browser - [ {0} ]", Path.GetFileName(mFileName));
                }
                return "BSA Browser";
            }
        }

        public string FilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(mFileName))
                {
                    return mFileName;
                }
                return string.Empty;
            }
        }

        public bool IsProcessing
        {
            get => mIsProcessing;
            set { SetProperty(ref mIsProcessing, value); }
        }

        public bool IsFileOpened
        {
            get => mNode != null;
        }

        public bool IsHasFiles
        {
            get => mNode != null && mNode.Size > 0;
        }

        public bool HasSelected
        {
            get => mSelectedNodes != null && mSelectedNodes.Count > 0;
        }

        public ArchiveNode Node
        {
            get => mNode;
            set { SetProperty(ref mNode, value); }
        }

        public ArchiveNode TreeSelected
        {
            get => mTreeSelected;
            set 
            { 
                SetProperty(ref mTreeSelected, value);
                PostPropertyChanged("Nodes");
            }
        }
        
        public IEnumerable<ArchiveNode> Nodes
        {
            get => mTreeSelected;
        }

        public IList<ArchiveNode> SelectedNodes
        {
            get => mSelectedNodes;
            set 
            { 
                SetProperty(ref mSelectedNodes, value);
                PostPropertyChanged("HasSelected");
            }
        }

        public ICommand SearchCommand
        {
            get => new DelegateCommand(OnSearch);
        }

        public ObservableCollection<ArchiveNode> FoundNodes
        {
            get => mFoundNodes;
        }

        public string Content
        {
            get => mContent;
            private set { SetProperty(ref mContent, value); }
        }
        #endregion

        #region Constructor
        public MainViewModel()
        {
        }
        #endregion

        #region Methods
        public void InitializeRecentFiles(MenuItem miRecentFiles)
        {
            var store = new RecentFilesPersistence(Config.AppDataConfigDir);
            recentFilesMenu = new RecentFilesMenu(store);
            recentFilesMenu.RecentFileSelected += RecentFile_Selected;
            recentFilesMenu.Initialize(miRecentFiles);
        }

        private void RecentFile_Selected(object sender, RecentFileSelectedEventArgs e)
        {
            CloseFile();
            OpenFile(e.FileName);
        }

        public async void OpenFile(string filePath = null)
        {
            if (IsProcessing) return;
            IsProcessing = true;

            string fileName = filePath;
            if (string.IsNullOrEmpty(fileName))
            {
                if (fileDialog.ShowDialog(Application.Current.MainWindow) == true)
                {
                    fileName = fileDialog.FileName;
                }
            }

            if (mArchiveFile != null)
            {
                mArchiveFile.Dispose();
            }
            if (!File.Exists(fileName))
            {
                IsProcessing = false;
                return;
            }

            mArchiveFile = new ArchiveFile(fileName);

            var node = await Task.Factory.StartNew(() =>
            {
                mArchiveFile.Load();
                Thread.Sleep(200);
                return mArchiveFile.Node;
            });

            if (node != null)
            {
                recentFilesMenu.AddRecentFile(fileName);
                mFileName = fileName;
                Node = node;
                Node.IsExpanded = true;
                TreeSelected = null;

                PostPropertyChanged(fileProperties);
            }

            IsProcessing = false;
        }

        public void OnFileDrop(DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
            {
                var filePath = files.Where(f => f.ToLowerInvariant().EndsWith(".bsa") || f.ToLowerInvariant().EndsWith(".ba2")).ToArray().FirstOrDefault();
                OpenFile(filePath);
            }
        }

        public void CloseFile()
        {
            TreeSelected = null;
            Node = null;
            if (mArchiveFile != null)
            {
                mArchiveFile.Dispose();
                mArchiveFile = null;
            }
            mFileName = null;

            PostPropertyChanged(fileProperties);
        }

        public async void Extract(string folderPath = null)
        {
            if (mSelectedNodes == null) return;
            if (mSelectedNodes.Count == 0) return;

            string dirPath = folderPath;
            if (string.IsNullOrEmpty(folderPath))
            {
                if (saveFolderDialog.ShowDialog(Application.Current.MainWindow) == true)
                {
                    dirPath = saveFolderDialog.ResultPath;
                }
            }

            IsProcessing = true;
            try
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                var list = mSelectedNodes.ToArray();

                await Task.Factory.StartNew(() => 
                {
                    foreach(var n in list)
                    {
                        if (n.IsFolder)
                        {
                            foreach(var fn in n.Files)
                            {
                                var name = fn.Path.Substring(n.DirPath.Length).TrimStart('\\');
                                string destFilePath = Path.Combine(dirPath, name);
                                mArchiveFile.ExtractTo(fn.Entry, destFilePath);
                            }
                        }
                        else
                        {
                            string destFilePath = Path.Combine(dirPath, n.Name);
                            mArchiveFile.ExtractTo(n.Entry, destFilePath);
                        }
                    }
                });
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            IsProcessing = false;
        }

        public async void ExtractAll(string folderPath = null)
        {
            string dirPath = folderPath;
            if (string.IsNullOrEmpty(folderPath))
            {
                if (saveFolderDialog.ShowDialog(Application.Current.MainWindow) == true)
                {
                    dirPath = saveFolderDialog.ResultPath;
                }
            }

            IsProcessing = true;
            try
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                await Task.Factory.StartNew(() =>
                {
                    mArchiveFile.ExtractAll(dirPath);
                });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            IsProcessing = false;
        }

        public async void CopySelected()
        {
            if (mSelectedNodes == null) return;
            if (mSelectedNodes.Count == 0) return;

            try
            {
                string tempPath = Path.GetTempFileName();
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                var list = mSelectedNodes.ToArray();

                var result = await Task.Factory.StartNew(() =>
                {
                    List<string> files = new();
                    foreach (var n in list)
                    {
                        if (n.IsFolder)
                        {
                            foreach (var fn in n.Files)
                            {
                                var name = fn.Path.Substring(n.DirPath.Length).TrimStart('\\');
                                string destFilePath = Path.Combine(tempPath, name);
                                mArchiveFile.ExtractTo(fn.Entry, destFilePath);
                            }

                            string destDirPath = Path.Combine(tempPath, n.Name);
                            files.Add(destDirPath);
                        }
                        else
                        {
                            string destFilePath = Path.Combine(tempPath, n.Name);
                            mArchiveFile.ExtractTo(n.Entry, destFilePath);
                            files.Add(destFilePath);
                        }
                    }
                    return files;
                });

                Clipboard.Clear();
                if (!string.IsNullOrEmpty(mCopyTempPath))
                {
                    DeleteFolder(mCopyTempPath);
                }

                mCopyObject = new DataObject(DataFormats.FileDrop, result.ToArray());
                Clipboard.SetDataObject(mCopyObject);

                mCopyTempPath = tempPath;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        private void OnSearch(object parameter)
        {
            if (parameter is Controls.SearchTextBox textBox)
            {
                var criteria = textBox.Text;
                if (!string.IsNullOrEmpty(criteria))
                {
                    Content = SearchContent;
                    DoSearch(criteria);
                }
                else
                {
                    Content = MainContent;
                }
            }
        }

        private async void DoSearch(string criteria)
        {
            FoundNodes.Clear();
            try
            {
                var searchString = WildcardPattern.Escape(criteria).Replace("`*", "*");
                var pattern = new WildcardPattern($"*{searchString}*", WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

                var nodes = await Task.Factory.StartNew(() =>
                {
                    var files = new List<ArchiveNode>();
                    foreach (var entry in mArchiveFile)
                    {
                        if (pattern.IsMatch(entry.fullPath))
                        {
                            files.Add(new ArchiveNode(entry));
                        }
                    }

                    return files;
                });

                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        FoundNodes.Add(node);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        private void DeleteFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(mCopyTempPath))
            {
                Clipboard.Clear();
                DeleteFolder(mCopyTempPath);
            }

            mTreeSelected = null;
            mNode = null;
            if (mArchiveFile != null)
            {
                mArchiveFile.Dispose();
                mArchiveFile = null;
            }
            mFileName = null;
        }
        #endregion
    }
}

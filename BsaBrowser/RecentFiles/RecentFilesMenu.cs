using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BsaBrowser.RecentFiles
{
    public sealed class RecentFilesMenu : IRecentFilesMenu, IDisposable
    {
        #region Variables
        private const int MAX_DISPLAYED_FILE_PATH_LENGTH = 100;
        private static string _persistenceDirectory = null;
        private static readonly string CLEAR_LIST_TEXT = "_Clear List";

        public event EventHandler<RecentFileSelectedEventArgs> RecentFileSelected;

        private readonly IRecentFilesStore _persistence;
        private readonly ICommand _openRecentFileCommand;
        private readonly ICommand _clearRecentFilesCommand;

        private MenuItem _miRecentFiles;

        private readonly int _maxFiles;
        private readonly string _clearListText;
        #endregion

        #region Constructors
        public RecentFilesMenu()
            : this(GetPersistenceDirectoryPath())
        { }

        public RecentFilesMenu(string persistenceDirectoryPath, int maxFiles = 10, string clearListText = null)
        {
            if (maxFiles < 1 || maxFiles > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFiles));
            }

            _maxFiles = maxFiles;
            _clearListText = string.IsNullOrWhiteSpace(clearListText) ? CLEAR_LIST_TEXT : clearListText;

            _persistence = new RecentFilesPersistence(persistenceDirectoryPath);
            _openRecentFileCommand = new OpenRecentFile(new Action<object>(OpenRecentFile_Executed));
            _clearRecentFilesCommand = new ClearRecentFiles(new Action(ClearRecentFiles_Executed));
        }

        public RecentFilesMenu(IRecentFilesStore persistence, int maxFiles = 10, string clearListText = null)
        {
            if (persistence == null)
            {
                throw new ArgumentNullException("persistence");
            }
            if (maxFiles < 1 || maxFiles > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFiles));
            }

            _maxFiles = maxFiles;
            _clearListText = string.IsNullOrWhiteSpace(clearListText) ? CLEAR_LIST_TEXT : clearListText;

            _persistence = persistence;
            _openRecentFileCommand = new OpenRecentFile(new Action<object>(OpenRecentFile_Executed));
            _clearRecentFilesCommand = new ClearRecentFiles(new Action(ClearRecentFiles_Executed));
        }
        #endregion

        #region Public Methods
        public static string GetPersistenceDirectoryPath()
        {
            if (!string.IsNullOrEmpty(_persistenceDirectory)) return _persistenceDirectory;

            var assm = System.Reflection.Assembly.GetEntryAssembly();
            if (assm != null)
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assm.Location);
                var appName = versionInfo.ProductName ?? assm.GetName()?.Name ?? "";
                var companyName = versionInfo.CompanyName ?? "";

                var configDir = Path.Combine(appDataPath, companyName, appName);
                if (!Directory.Exists(configDir))
                {
                    try
                    {
                        var dirInfo = Directory.CreateDirectory(configDir);
                        if (dirInfo.Exists)
                        {
                            _persistenceDirectory = dirInfo.FullName;
                        }
                    }
                    catch { }
                }
                else
                {
                    _persistenceDirectory = configDir;
                }

            }
            return _persistenceDirectory;
        }

        public void Initialize(MenuItem miRecentFiles)
        {
            if (miRecentFiles is null)
            {
                throw new ArgumentNullException(nameof(miRecentFiles));
            }

            _miRecentFiles = miRecentFiles;
            _miRecentFiles.Loaded += RecentFiles_Loaded;
        }

        public void AddRecentFile(string fileName)
        {
            lock (_persistence)
            {
                _persistence.AddRecentFile(fileName, _maxFiles);
            }
        }

        public void RemoveRecentFile(string fileName)
        {
            _persistence.RemoveRecentFile(fileName);
        }

        public async Task<string> GetMostRecentFileAsync()
        {
            var recentFiles = await _persistence.GetRecentFiles();

            lock (recentFiles)
            {
                return recentFiles.FirstOrDefault();
            }
        }

        public void Dispose()
        {
            if (_persistence is RecentFilesPersistence persistence)
                persistence.Dispose();
        }
        #endregion

        #region Private Methods
        private async void RecentFiles_Loaded(object sender, RoutedEventArgs e)
        {
            _miRecentFiles.Items.Clear();
            var recentFiles = await _persistence.GetRecentFiles();

            if (recentFiles.Count == 0)
            {
                _miRecentFiles.IsEnabled = false;
            }
            else
            {
                try
                {
                    _miRecentFiles.IsEnabled = true;

                    lock (recentFiles)
                    {
                        for (int i = 0; i < recentFiles.Count; i++)
                        {
                            string currentFile = recentFiles[i];

                            if (string.IsNullOrWhiteSpace(currentFile))
                            {
                                continue;
                            }

                            var mi = new MenuItem
                            {
                                Header = GetMenuItemHeaderFromFilename(currentFile, i),
                                Command = _openRecentFileCommand,
                                CommandParameter = recentFiles[i],

                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                VerticalContentAlignment = VerticalAlignment.Stretch,
                            };

                            _miRecentFiles.Items.Add(mi);
                        }
                    }

                    var menuItemClearList = new MenuItem
                    {
                        Header = _clearListText,
                        Command = _clearRecentFilesCommand
                    };

                    _miRecentFiles.Items.Add(new Separator());
                    _miRecentFiles.Items.Add(menuItemClearList);
                }
                catch
                {
                    _miRecentFiles.Items.Clear();
                    _miRecentFiles.IsEnabled = false;

                    ClearRecentFiles_Executed();
                }
            }
        }

        static string GetMenuItemHeaderFromFilename(string fileName, int i)
        {
            if (fileName.Length > MAX_DISPLAYED_FILE_PATH_LENGTH)
            {
                const int QUARTER_DISPLAYED_FILE_PATH_LENGTH = MAX_DISPLAYED_FILE_PATH_LENGTH / 4;
                const int THREE_QUARTER_DISPLAYED_FILE_PATH_LENGTH = QUARTER_DISPLAYED_FILE_PATH_LENGTH * 3;

                fileName = string.Concat(fileName.Substring(0, QUARTER_DISPLAYED_FILE_PATH_LENGTH - 3),
                                  "...",
                                  fileName.Substring(fileName.Length - THREE_QUARTER_DISPLAYED_FILE_PATH_LENGTH));
            }

            if (i < 9)
            {
                fileName = "_" + (i + 1).ToString(CultureInfo.InvariantCulture) + ": " + fileName;
            }
            else if (i == 9)
            {
                fileName = "1_0: " + fileName;
            }

            return fileName;
        }
        #endregion

        #region Command-Execute-Handler

        [ExcludeFromCodeCoverage]
        private void OpenRecentFile_Executed(object fileName)
        {
            if (RecentFileSelected != null && fileName is string s)
            {
                RecentFileSelected.Invoke(this, new RecentFileSelectedEventArgs(s));
            }
        }

        [ExcludeFromCodeCoverage]
        private void ClearRecentFiles_Executed()
        {
            _persistence.ClearRecentFiles();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BsaBrowser.RecentFiles
{
    internal sealed class RecentFilesPersistence : IRecentFilesStore
    {
        #region Variables
        private const int MUTEX_TIMEOUT = 3000;

        private readonly string _fileName;
        private readonly Mutex _mutex;
        private readonly List<string> recentFiles = new List<string>();
        #endregion

        #region Constructor
        public RecentFilesPersistence(string persistenceDirectoryPath)
        {
            if (persistenceDirectoryPath is null)
            {
                throw new ArgumentNullException(persistenceDirectoryPath);
            }

            if (!Utility.IsPathDirectory(persistenceDirectoryPath))
            {
                throw new ArgumentException(persistenceDirectoryPath);
            }

            _fileName = Path.Combine(persistenceDirectoryPath, $"{Environment.MachineName}.{Environment.UserName}.RF.txt");
            _mutex = new Mutex(false, $"Global\\{_fileName.Replace('\\', '_')}");
        }
        #endregion

        #region Methods
        public Task LoadAsync()
        {
            return Task.Run(() =>
            {
                if (File.Exists(_fileName))
                {
                    string[] arr = Array.Empty<string>();
                    try
                    {
                        if (_mutex.WaitOne(MUTEX_TIMEOUT))
                        {
                            try
                            {
                                arr = File.ReadAllLines(_fileName);
                            }
                            catch { return; }
                            finally
                            {
                                _mutex.ReleaseMutex();
                            }
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }

                    lock (recentFiles)
                    {
                        recentFiles.Clear();
                        recentFiles.AddRange(arr);
                    }
                }
            });
        }

        public Task SaveAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_mutex.WaitOne(MUTEX_TIMEOUT))
                    {
                        try
                        {
                            lock (recentFiles)
                            {
                                File.WriteAllLines(_fileName, recentFiles);
                            }
                        }
                        catch { }
                        finally
                        {
                            _mutex.ReleaseMutex();
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        public async Task<List<string>> GetRecentFiles()
        {
            await LoadAsync().ConfigureAwait(false);
            return recentFiles;
        }

        public async void AddRecentFile(string fileName, int maxFiles)
        {
            try
            {
                fileName = Path.GetFullPath(fileName);
            }
            catch
            {
                return;
            }

            lock (recentFiles)
            {
                recentFiles.Remove(fileName);
                recentFiles.Insert(0, fileName);

                if (recentFiles.Count > maxFiles)
                {
                    recentFiles.RemoveAt(maxFiles);
                }
            }
            await SaveAsync().ConfigureAwait(false);
        }

        public async void RemoveRecentFile(string fileName)
        {
            bool result;
            lock (recentFiles)
            {
                result = recentFiles.Remove(fileName);
            }

            if (result)
            {
                await SaveAsync().ConfigureAwait(false);
            }
        }

        public async void ClearRecentFiles()
        {
            lock (recentFiles)
            {
                recentFiles.Clear();
            }
            await SaveAsync();
        }

        public void Dispose() => _mutex.Dispose();
        #endregion
    }
}

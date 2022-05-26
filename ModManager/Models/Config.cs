using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using ModManager.GameModules;

namespace ModManager.Models
{
    public class Config
    {
        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string appDataConfigDir = string.Empty;

        public static Config DefaultConfig = GetDefaultConfig();

        private static bool HasWriteAccess(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) return false;

            var isInRoleWithAccess = false;
            var accessRights = FileSystemRights.Write;

            try
            {
                var di = new DirectoryInfo(dirPath);
                if (!di.Exists) return false;

                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                var currentUser = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentUser);
                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }

        public static string AppDir = AppDomain.CurrentDomain.BaseDirectory;

        public static string AppDataConfigDir
        {
            get
            {
                if (!string.IsNullOrEmpty(appDataConfigDir)) return appDataConfigDir;
                
                var assm = System.Reflection.Assembly.GetEntryAssembly();
                if (assm != null)
                {
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
                                appDataConfigDir = dirInfo.FullName;
                            }
                        }
                        catch { }
                    }
                }
                return appDataConfigDir;
            }
        }

        public Config()
        { 
        }         

        private static Config GetDefaultConfig()
        {
            var config = new Config
            {
                Name = "Skyrim Special Edition",
#if DEBUG
                PluginFile = @"Plugins.txt",
#else
                PluginFile = "",
#endif
                InstallFolder = new InstallFolderPath
                {
                    Path = "",
                    Registry = new RegistryPath
                    {
                        Path = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Bethesda Softworks\Skyrim Special Edition",
                        KeyName = "Installed Path"
                    }
                }
            };

            return config;
        }

        public static Config Load(string fileName = "config.json")
        {
            var defConfig = DefaultConfig;
            
            var dirList = new List<string>();
            dirList.Add(AppDir);
            dirList.Add(AppDataConfigDir);

            foreach (string dir in dirList)
            {
                var filePath = Path.Combine(dir, fileName);
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    try
                    {
                        using (var reader = new StreamReader(fileInfo.OpenRead()))
                        {
                            var json = reader.ReadToEnd();
                            var config = JsonSerializer.Deserialize<Config>(json);
                            if (config != null)
                            {
                                config.Initialize();
                                return config;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }
            }

            foreach (string dir in dirList)
            {
                try
                {
                    if (HasWriteAccess(dir))
                    {
                        var filePath = Path.Combine(dir, fileName);
                        var fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            fileInfo.Delete();
                        }

                        using (var stream = fileInfo.Create())
                        {
                            JsonSerializer.Serialize(stream, defConfig, new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                            });
                        }
                        break;
                    }                 
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            defConfig.Initialize();
            return defConfig;
        }

        [JsonIgnore]
        public GameSettings? Settings { get; private set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonIgnore]
        public string AppDataPath { get => appDataPath; }

        [JsonIgnore]
        public string GamePath { get; private set; } = string.Empty;

        [JsonIgnore]
        public GameID GameId
        {
            get {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    if (this.Name == "Morrowind")
                        return GameID.Morrowind;
                    else if (this.Name == "Oblivion")
                        return GameID.Oblivion;
                    else if (this.Name == "Skyrim")
                        return GameID.Skyrim;
                    else if (this.Name == "Skyrim Special Edition")
                        return GameID.SkyrimSE;
                    else if (this.Name == "Skyrim VR")
                        return GameID.SkyrimVR;
                    else if (this.Name == "Fallout3")
                        return GameID.Fallout3;
                    else if (this.Name == "FalloutNV")
                        return GameID.FalloutNV;
                    else if (this.Name == "Fallout4")
                        return GameID.Fallout4;
                    else if (this.Name == "Fallout4 VR")
                        return GameID.Fallout4VR;
                }
                return GameID.UnKnown;
            }
        }

        [JsonPropertyName("plugin")]
        public string? PluginFile { get; set; }

        [JsonPropertyName("gamePath")]
        public InstallFolderPath? InstallFolder { get; set; }

        private void Initialize()
        {
            if (this.InstallFolder != null)
            {
                if (!string.IsNullOrEmpty(this.InstallFolder.Path)
                    && Directory.Exists(this.InstallFolder.Path))
                {
                    this.GamePath = this.InstallFolder.Path;
                }
                else
                {
                    if (!string.IsNullOrEmpty(this.InstallFolder.Registry?.Path)
                        && !string.IsNullOrEmpty(this.InstallFolder.Registry?.KeyName))
                    {
                        var regPath = this.InstallFolder.Registry.Path;
                        var regKey = this.InstallFolder.Registry.KeyName;

                        var installPath = Registry.GetValue(regPath, regKey, null) as string;
                        if (!string.IsNullOrEmpty(installPath)
                            && Directory.Exists(installPath))
                        {
                            this.GamePath = installPath;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(this.GamePath))
                {
                    this.Settings = new GameSettings(this.GameId, this.GamePath);
                }
            } 
        }

        public FileInfo? GetPluginFile()
        {
            if (this.Settings == null) return null;
            FileInfo? fileInfo = null;
            if (!string.IsNullOrEmpty(this.PluginFile))
            {
                fileInfo = new FileInfo(this.PluginFile);
                if (fileInfo.Exists) return fileInfo;
            }

            fileInfo = new FileInfo(this.Settings.PluginFilePath);
            return fileInfo?.Exists == true ? fileInfo : null;
        }

        private bool IsModulesDataDir(DirectoryInfo dirInfo)
        {
            if (dirInfo.Exists && this.Settings != null)
            {
                var systemFile = this.Settings.MasterFile;
                if (!string.IsNullOrEmpty(systemFile))
                {
                    var systemFilePath = Path.Combine(dirInfo.FullName, systemFile);
                    if (File.Exists(systemFilePath)) return true;
                }
            }
            return false;
        }

        public DirectoryInfo? GetModulesDataDir()
        {
            if (this.Settings == null) return null;
            var dirInfo = new DirectoryInfo(this.Settings.PluginsDirectory);
            return this.IsModulesDataDir(dirInfo) ? dirInfo : null;
        }

        public class InstallFolderPath
        {
            [JsonPropertyName("path")]
            public string? Path { get; set; }

            [JsonPropertyName("registry")]
            public RegistryPath? Registry { get; set; }
        }

        public class RegistryPath
        {
            [JsonPropertyName("path")]
            public string? Path { get; set; }

            [JsonPropertyName("key")]
            public string? KeyName { get; set; }
        }
    }
}

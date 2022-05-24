using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

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
                ModuleFolder = "Data",
                PluginFile = new PluginFilePath
                {
                    FolderName = "Skyrim Special Edition",
                    FileName = "Plugins.txt",
#if DEBUG
                    FullPath = @"bin\Fake\Plugins.txt"
#else
                    FullPath = ""
#endif
                },
                Format = new Formating
                {
                    Enable = new FormatExp
                    {
                        Pattern = @"^\*(.*?[\w\W])$",
                        Format = "*<fileName>"
                    },
                    Disable = new FormatExp
                    {
                        Pattern = @"^([\w].*?[\w\W])$",
                        Format = "<fileName>"
                    },
                    Comment = new FormatExp
                    {
                        Pattern = @"^#[\s]{0,}(.*?[\w\W])$",
                        Format = "# <comment>"
                    }
                },
                InstallFolder = new InstallFolderPath
                {
#if DEBUG
                    Path = @"bin\Fake\Data",
#else
                    Path = "",
#endif
                    Registry = new RegistryPath
                    {
                        Path = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Bethesda Softworks\Skyrim Special Edition",
                        KeyName = "Installed Path"
                    }
                }
            };

            config.SystemModules.Add("Skyrim.esm");
            config.SystemModules.Add("Update.esm");
            config.SystemModules.Add("Dawnguard.esm");
            config.SystemModules.Add("HearthFires.esm");
            config.SystemModules.Add("Dragonborn.esm");

            config.SystemModules.Add("ccBGSSSE001-Fish.esm");
            config.SystemModules.Add("ccQDRSSE001-SurvivalMode.esl");
            config.SystemModules.Add("ccBGSSSE037-Curios.esl");
            config.SystemModules.Add("ccBGSSSE025-AdvDSGS.esm");

            config.ModuleExtensions.Add("esm");
            config.ModuleExtensions.Add("esl");
            config.ModuleExtensions.Add("esp");

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
                            if (config != null) return config;
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
            return defConfig;
        }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonIgnore]
        public string AppDataPath { get => appDataPath; }

        [JsonPropertyName("plugin")]
        public PluginFilePath? PluginFile { get; set; }

        [JsonPropertyName("format")]
        public Formating? Format { get; set; }

        [JsonPropertyName("gamePath")]
        public InstallFolderPath? InstallFolder { get; set; }

        [JsonPropertyName("dataFolder")]
        public string? ModuleFolder { get; set; }

        [JsonPropertyName("moduleExts")]
        public List<string> ModuleExtensions { get; set; } = new List<string>();

        [JsonPropertyName("systemModules")]
        public List<string> SystemModules { get; set; } = new List<string>();

        public FileInfo? GetPluginFile()
        {
            if (this.PluginFile != null)
            {
                var filePath = string.Empty;

                if (!string.IsNullOrEmpty(this.PluginFile.FullPath)
                    && File.Exists(this.PluginFile.FullPath))
                {
                    filePath = this.PluginFile.FullPath;
                }
                else if (!string.IsNullOrEmpty(this.PluginFile.FolderName)
                    && !string.IsNullOrEmpty(this.PluginFile.FileName))
                {
                    filePath = Path.Combine(this.AppDataPath, this.PluginFile.FolderName, this.PluginFile.FileName);
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    var file = new FileInfo(filePath);
                    if (file.Exists)
                    {
                        return file;
                    }

                    try
                    {
                        file.CreateText().Dispose();
                        return file;
                    } 
                    catch(Exception e)
                    { 
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }
            }
            return null;
        }

        private bool IsModulesDataDir(DirectoryInfo dirInfo)
        {
            if (dirInfo.Exists)
            {
                var systemFile = this.SystemModules.FirstOrDefault();
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
            var dataDir = "Data";

            if (!string.IsNullOrEmpty(this.ModuleFolder))
            {
                var dirInfo = new DirectoryInfo(this.ModuleFolder);
                if (this.IsModulesDataDir(dirInfo))
                {
                    return dirInfo;
                }
                dataDir = this.ModuleFolder;
            }
            
            if (this.InstallFolder != null)
            {
                if (!string.IsNullOrEmpty(this.InstallFolder.Path))
                {
                    var dirInfo = new DirectoryInfo(this.InstallFolder.Path);
                    if (this.IsModulesDataDir(dirInfo))
                    {
                        return dirInfo;
                    }

                    var dataPath = Path.Combine(this.InstallFolder.Path, dataDir);
                    dirInfo = new DirectoryInfo(dataPath);

                    if (this.IsModulesDataDir(dirInfo))
                    {
                        return dirInfo;
                    }
                }

                if (!string.IsNullOrEmpty(this.InstallFolder.Registry?.Path)
                    && !string.IsNullOrEmpty(this.InstallFolder.Registry?.KeyName))
                {
                    var regPath = this.InstallFolder.Registry.Path;
                    var regKey = this.InstallFolder.Registry.KeyName;

                    var installPath = Registry.GetValue(regPath, regKey, null) as string;
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var dataPath = Path.Combine(installPath, dataDir);
                        var dirInfo = new DirectoryInfo(dataPath);

                        if (this.IsModulesDataDir(dirInfo))
                        {
                            return dirInfo;
                        }
                    }
                }
            }
            return null;
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

        public class PluginFilePath
        {
            [JsonPropertyName("folder")]
            public string? FolderName { get; set; }


            [JsonPropertyName("file")]
            public string? FileName { get; set; }

            [JsonPropertyName("path")]
            public string? FullPath { get; set; }
        }

        public class Formating
        {
            [JsonPropertyName("enable")]
            public FormatExp? Enable { get; set; }

            [JsonPropertyName("disable")]
            public FormatExp? Disable { get; set; }

            [JsonPropertyName("comment")]
            public FormatExp? Comment { get; set; }

            [JsonPropertyName("removeOnDisable")]
            public bool RemoveOnDisable { get; set; } = true;
        }

        public class FormatExp
        {
            [JsonPropertyName("pattern")]
            public string? Pattern { get; set; }

            [JsonPropertyName("format")]
            public string? Format { get; set; }
        }
    }
}

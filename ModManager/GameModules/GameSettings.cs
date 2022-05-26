using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class GameSettings
    {
        public static readonly string[] SKYRIM_HARDCODED_PLUGINS = {
            "Skyrim.esm", "Update.esm"
        };

        public static readonly string[] SKYRIM_SE_HARDCODED_PLUGINS = {
            "Skyrim.esm",
            "Update.esm",
            "Dawnguard.esm",
            "HearthFires.esm",
            "Dragonborn.esm"
        };

        public static readonly string[] SKYRIM_VR_HARDCODED_PLUGINS = {
            "Skyrim.esm",
            "Update.esm",
            "Dawnguard.esm",
            "HearthFires.esm",
            "Dragonborn.esm",
            "SkyrimVR.esm"
        };

        public static readonly string[] FALLOUT4_HARDCODED_PLUGINS = {
            "Fallout4.esm",
            "DLCRobot.esm",
            "DLCworkshop01.esm",
            "DLCCoast.esm",
            "DLCworkshop02.esm",
            "DLCworkshop03.esm",
            "DLCNukaWorld.esm",
            "DLCUltraHighResolution.esm"
        };

        public static readonly string[] FALLOUT4VR_HARDCODED_PLUGINS = {
            "Fallout4.esm", "Fallout4_VR.esm"
        };

        public static readonly string[] VALID_EXTENSIONS = {
            ".esm", ".esl", ".esu", ".esp"
        };

        private string[] m_implicitlyActivePlugins = new string[] { };
        private string[] m_loadOrderPlugins = new string[] { };

        public GameSettings(GameID gameId, string gamePath)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            this.Id = gameId;
            this.LocalPath = Path.Combine(appDataPath, AppDataFolderName);
            this.GamePath = gamePath;

            this.m_implicitlyActivePlugins = this.ReadImplicitlyActivePlugins();
            this.m_loadOrderPlugins = this.ReadLoadOrderPlugins();
        }

        public GameID Id { get; private set; }

        public string GamePath { get; private set; }

        public string LocalPath { get; private set; }

        public string MasterFile
        {
            get
            {
                switch (this.Id)
                {
                    case GameID.Morrowind:
                        return "Morrowind.esm";
                    case GameID.Oblivion:
                        return "Oblivion.esm";
                    case GameID.Skyrim:
                    case GameID.SkyrimSE:
                    case GameID.SkyrimVR:
                        return "Skyrim.esm";
                    case GameID.Fallout3:
                        return "Fallout3.esm";
                    case GameID.FalloutNV:
                        return "FalloutNV.esm";
                    case GameID.Fallout4:
                    case GameID.Fallout4VR:
                        return "Fallout4.esm";
                    default:
                        return string.Empty;
                }
            }
        }

        public string PluginFolderName
        {
            get
            {
                if (this.Id == GameID.Morrowind)
                    return "Data Files";
                else
                    return "Data";
            }
        }

        public string AppDataFolderName
        {
            get
            {
                switch (this.Id)
                {
                    case GameID.Morrowind:
                        return string.Empty;
                    case GameID.Oblivion:
                        return "Oblivion";
                    case GameID.Skyrim:
                        return "Skyrim";
                    case GameID.SkyrimSE:
                        return "Skyrim Special Edition";
                    case GameID.SkyrimVR:
                        return "Skyrim VR";
                    case GameID.Fallout3:
                        return "Fallout3";
                    case GameID.FalloutNV:
                        return "FalloutNV";
                    case GameID.Fallout4:
                        return "Fallout4";
                    case GameID.Fallout4VR:
                        return "Fallout4VR";
                    default:
                        return string.Empty;
                }
            }
        }

        public string LoadOrderPath
        {
            get
            {
                if (this.Id == GameID.Skyrim)
                    return Path.Combine(this.LocalPath, "loadorder.txt");
                else
                    return string.Empty;
            }
        }

        private string[] ReadLoadOrderPlugins()
        {
            var pluginNames = new List<string>();
            var fileName = this.LoadOrderPath;

            if (!string.IsNullOrEmpty(fileName))
            {
                if (File.Exists(fileName))
                {
                    using (var reader = File.OpenText(fileName))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                pluginNames.Add(line);
                            }
                        }
                    }
                }
            }

            return pluginNames.ToArray();
        }

        public string[] LoadOrderPlugins
        {
            get => this.m_loadOrderPlugins;
        }

        public string PluginFilePath
        {
            get
            {
                if (this.Id == GameID.Oblivion)
                {
                    var iniFilePath = Path.Combine(this.GamePath, "Oblivion.ini");
                    if (File.Exists(iniFilePath))
                        return Path.Combine(this.LocalPath, "Plugins.txt");
                    else
                        return Path.Combine(this.GamePath, "Plugins.txt");
                }
                else if (this.Id == GameID.Morrowind)
                {
                    return Path.Combine(this.GamePath, "Morrowind.ini");
                }
                else
                    return Path.Combine(this.LocalPath, "Plugins.txt");
            }
        }

        public string CCCFilePath
        {
            get
            {
                if (Id == GameID.Fallout4)
                    return Path.Combine(GamePath, "Fallout4.ccc");
                else if (Id == GameID.SkyrimSE)
                    return Path.Combine(GamePath, "Skyrim.ccc");
                return string.Empty;
            }
        }

        public string[] HardcodedPlugins
        {
            get
            {
                switch (Id)
                {
                    case GameID.Skyrim:
                        return SKYRIM_HARDCODED_PLUGINS;
                    case GameID.SkyrimSE:
                        return SKYRIM_SE_HARDCODED_PLUGINS;
                    case GameID.SkyrimVR:
                        return SKYRIM_VR_HARDCODED_PLUGINS;
                    case GameID.Fallout4:
                        return FALLOUT4_HARDCODED_PLUGINS;
                    case GameID.Fallout4VR:
                        return FALLOUT4VR_HARDCODED_PLUGINS;
                    default:
                        return new string[] { };
                }
            }
        }

        private string[] ReadImplicitlyActivePlugins()
        {
            var pluginNames = new List<string>();
            var cccFilePath = CCCFilePath;
            if (!string.IsNullOrEmpty(cccFilePath))
            {
                if (File.Exists(cccFilePath))
                {
                    using (var reader = File.OpenText(cccFilePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (!string.IsNullOrEmpty(line))
                            {
                                pluginNames.Add(line);
                            }
                        }
                    }
                }
            }
            return pluginNames.ToArray();
        }

        public string[] ImplicitlyActivePlugins
        {
            get => this.m_implicitlyActivePlugins;
        }

        public string PluginsDirectory
        {
            get => Path.Combine(GamePath, PluginFolderName);
        }

        public static string Ghost(string plugin)
        {
            if (string.IsNullOrEmpty(plugin)) return string.Empty;
            if (!plugin.ToLower().EndsWith(".ghost"))
                return plugin + ".ghost";
            return plugin;
        }

        public static string UnGhost(string plugin)
        {
            if (string.IsNullOrEmpty(plugin)) return string.Empty;
            if (plugin.ToLower().EndsWith(".ghost"))
                return plugin.Substring(0, plugin.Length - 6);
            return plugin;
        }

        public bool IsImplicitlyActive(string plugin)
        {
            if (string.IsNullOrEmpty(plugin)) return false;
            var fileName = UnGhost(plugin);

            if (this.HardcodedPlugins.Contains(fileName,
              StringComparer.InvariantCultureIgnoreCase))
                return true;

            return this.m_implicitlyActivePlugins.Contains(fileName,
              StringComparer.InvariantCultureIgnoreCase);
        }

        public bool IsValidExtension(string plugin)
        {
            if (string.IsNullOrEmpty(plugin) || plugin.Length < 4) return false;
            var name = UnGhost(plugin);
            name = name.Substring(name.Length - 4);
            return VALID_EXTENSIONS.Contains(name, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
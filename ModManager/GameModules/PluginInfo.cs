using System;
using System.Linq;

namespace ModManager.GameModules
{
    public class PluginInfo : IComparable<PluginInfo>
    {
        public PluginInfo(PluginFile plugin, GameSettings settings, int pluginsTxtIdx)
        {
            if (plugin.File != null)
            {
                var file = plugin.File;
                this.OriginalName = file.Name;
                this.Name = GameSettings.UnGhost(file.Name);
                this.Extension = this.Name.Substring(this.Name.Length - 3);

                var lowerExt = this.Extension.ToLower();
                this.ESM = lowerExt == "esm";
                this.ESP = lowerExt == "esp";
                this.ESU = lowerExt == "esu";
                this.ESL = lowerExt == "esl";

                this.DateTime = file.LastWriteTime;
                this.IsMaster = this.Name == settings.MasterFile;
                this.Dependencies = plugin.Dependencies;

                var lowerName = this.Name.ToLower();

                if (settings.HardcodedPlugins.Length > 0)
                {
                    var idx = Array.FindIndex(settings.HardcodedPlugins, p => p.ToLower() == lowerName);
                    this.OfficialIndex = idx > -1 ? idx : int.MaxValue;
                }

                if (settings.ImplicitlyActivePlugins.Length > 0)
                {
                    var idx = Array.FindIndex(settings.ImplicitlyActivePlugins, p => p.ToLower() == lowerName);
                    this.CCIndex = idx > -1 ? idx : int.MaxValue;
                }

                this.PluginsTxtIndex = pluginsTxtIdx > -1 ? pluginsTxtIdx : int.MaxValue;

                if (settings.LoadOrderPlugins.Length > 0)
                {
                    var idx = Array.FindIndex(settings.LoadOrderPlugins, p => p.ToLower() == lowerName);
                    this.LoadOrderTxtIndex = idx > -1 ? idx : int.MaxValue;
                }
            }
        }

        public string OriginalName { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string Extension { get; private set; } = string.Empty;

        public bool ESM { get; private set; }

        public bool ESP { get; private set; }

        public bool ESU { get; private set; }

        public bool ESL { get; private set; }

        public bool IsESM { get => ESM || ESL; }

        public bool IsMaster { get; private set; }

        public DateTime DateTime { get; private set; } = DateTime.Now;

        public string[] Dependencies { get; private set; } = new string[] { };

        public int OfficialIndex { get; private set; } = int.MaxValue;

        public int CCIndex { get; private set; } = int.MaxValue;

        public int PluginsTxtIndex { get; private set; } = int.MaxValue;

        public int LoadOrderTxtIndex { get; private set; } = int.MaxValue;

        public bool IsDepandOn(PluginInfo other)
        {
            if (this.Dependencies.Length > 0)
            {
                return this.Dependencies.Contains(other.Name,
                  StringComparer.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public override string ToString()
        {
            return this.OriginalName;
        }

        public int CompareTo(PluginInfo? other)
        {
            if (other == null) return -1;

            var a = this;
            var b = other;

            if (a.Name == b.Name) return 0;
            if (a.IsMaster) return -1;
            if (b.IsMaster) return 1;

            if (b.IsDepandOn(a)) return -1;
            if (a.IsDepandOn(b)) return 1;

            var result = a.OfficialIndex.CompareTo(b.OfficialIndex);
            if (result == 0)
            {
                result = a.CCIndex.CompareTo(b.CCIndex);
                if (result == 0)
                {
                    if (a.IsESM == b.IsESM)
                    {
                        result = a.PluginsTxtIndex.CompareTo(b.PluginsTxtIndex);
                        if (result == 0)
                        {
                            result = a.DateTime.CompareTo(b.DateTime);
                            if (result == 0)
                            {
                                result = a.Name.CompareTo(b.Name);
                                if (result == 0)
                                {
                                    result = a.GetHashCode().CompareTo(b.GetHashCode());
                                }
                            }
                        }
                    }
                    else
                    {
                        if (a.IsESM)
                            result = -1;
                        else
                            result = 1;
                    }
                }
            }
            return result;
        }

        // Define the is greater than operator.
        public static bool operator >(PluginInfo operand1, PluginInfo operand2)
        {
            return operand1.CompareTo(operand2) > 0;
        }

        // Define the is less than operator.
        public static bool operator <(PluginInfo operand1, PluginInfo operand2)
        {
            return operand1.CompareTo(operand2) < 0;
        }

        // Define the is greater than or equal to operator.
        public static bool operator >=(PluginInfo operand1, PluginInfo operand2)
        {
            return operand1.CompareTo(operand2) >= 0;
        }

        // Define the is less than or equal to operator.
        public static bool operator <=(PluginInfo operand1, PluginInfo operand2)
        {
            return operand1.CompareTo(operand2) <= 0;
        }
    }
}
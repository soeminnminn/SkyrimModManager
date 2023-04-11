using System;
using System.Linq;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class PluginInfo : IComparable<PluginInfo>
    {
        #region Constructor
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
                this.Dependencies = plugin.Dependencies;

                this.IsSystemMaster = this.Name == settings.MasterFile;
                this.IsImplicit = settings.IsImplicitlyActive(this.Name);

                this.HasMasterFlag = plugin.HasMasterFlag;
                this.HasLightFlag = plugin.HasLightFlag;

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

                this.Author = plugin.Author;
                this.Description = plugin.Description;
                this.Localized = plugin.Localized;
            }
        }
        #endregion

        #region Properties
        public string OriginalName { get; private set; } = string.Empty;

        public string Name { get; private set; } = string.Empty;

        public string Extension { get; private set; } = string.Empty;

        public string Author { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public bool Localized { get; private set; }

        public bool IsSystemMaster { get; private set; }

        public bool IsImplicit { get; private set; }

        public bool HasMasterFlag { get; private set; }

        public bool HasLightFlag { get; private set; }

        public bool ESM { get; private set; }

        public bool ESP { get; private set; }

        public bool ESU { get; private set; }

        public bool ESL { get; private set; }

        public bool IsESM { get => ESM || ESL || HasLightFlag; }

        public DateTime DateTime { get; private set; } = DateTime.Now;

        public string[] Dependencies { get; private set; } = new string[] { };

        public int OfficialIndex { get; private set; } = int.MaxValue;

        public int CCIndex { get; private set; } = int.MaxValue;

        public int PluginsTxtIndex { get; private set; } = int.MaxValue;

        public int LoadOrderTxtIndex { get; private set; } = int.MaxValue;

        public bool MissingMaster { get; private set; }
        #endregion

        #region Methods
        public bool IsDepandOn(PluginInfo other)
        {
            if (other == null) return false;

            if (this.Dependencies.Length > 0)
            {
                return this.Dependencies.Contains(other.Name,
                  StringComparer.InvariantCultureIgnoreCase);
            }
            return false;
        }

        public void CheckMasterFilesMissing(string[] source)
        {
            if (this.Dependencies.Length > 0)
            {
                if (source.Length > 0)
                {
                    var containCount = this.Dependencies.Where(x => source.Contains(x, StringComparer.InvariantCultureIgnoreCase)).Count();
                    this.MissingMaster = containCount != this.Dependencies.Length;
                }
                else
                {
                    this.MissingMaster = true;
                }
            }
        }

        public override string ToString()
        {
            return this.OriginalName;
        }

        public int CompareTo(PluginInfo other)
        {
            if (other == null) return -1;

            var a = this;
            var b = other;

            if (a.Name == b.Name) return 0;

            if (a.IsSystemMaster && !b.IsSystemMaster) return -1;
            if (!a.IsSystemMaster && b.IsSystemMaster) return 1;

            if (a.IsImplicit && !b.IsImplicit) return -1;
            if (!a.IsImplicit && b.IsImplicit) return 1;

            if (a.HasMasterFlag && !b.HasMasterFlag) return -1;
            if (!a.HasMasterFlag && b.HasMasterFlag) return 1;

            if (!a.MissingMaster && b.MissingMaster) return -1;
            if (a.MissingMaster && !b.MissingMaster) return 1;

            if (!a.IsDepandOn(b) && b.IsDepandOn(a)) return -1;
            if (a.IsDepandOn(b) && !b.IsDepandOn(a)) return 1;

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
                                result = StringComparer.InvariantCultureIgnoreCase.Compare(a.Name, b.Name);
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
        #endregion

        #region Operators
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
        #endregion
    }
}
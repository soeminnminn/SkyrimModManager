using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BsaBrowser.Models
{
    internal class Config
    {
        #region Variables
        private static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string appDataConfigDir = string.Empty;
        #endregion

        #region Constructors
        public Config() 
        {
        }
        #endregion

        #region Properties
        public static string AppDataConfigDir
        {
            get
            {
                if (!string.IsNullOrEmpty(appDataConfigDir)) return appDataConfigDir;

                var assm = Assembly.GetEntryAssembly();
                if (assm != null)
                {
                    string appName = assm.GetName().Name;
                    var titleAttr = assm.GetCustomAttribute(typeof(AssemblyTitleAttribute)) as AssemblyTitleAttribute;
                    if (titleAttr != null && !string.IsNullOrEmpty(titleAttr.Title))
                    {
                        appName = titleAttr.Title;
                    }

                    string companyName = string.Empty;
                    var companyAttr = assm.GetCustomAttribute(typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                    if (companyAttr != null && !string.IsNullOrEmpty(companyAttr.Company))
                    {
                        companyName = companyAttr.Company;
                    }

                    string configDir = Path.Combine(appDataPath, companyName, appName);
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
                    else
                    {
                        appDataConfigDir = configDir;
                    }

                }
                return appDataConfigDir;
            }
        }
        #endregion

        #region Methods
        #endregion
    }
}

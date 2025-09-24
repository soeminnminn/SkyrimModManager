using System;
using System.Collections.Generic;

namespace ModManager.GameModules
{
    public class LoadOrderComparer : IComparer<PluginInfo>
    {
        #region Constructors
        public LoadOrderComparer()
        { }
        #endregion

        #region Methods
        public int Compare(PluginInfo a, PluginInfo b)
        {
            var result = 0;
            if (a != null && b == null) result = -1;
            if (a == null && b != null) result = 1;

            if (result == 0 && a != null) result = a.CompareTo(b);
            return result;
        }
        #endregion
    }
}
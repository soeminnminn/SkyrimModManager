﻿using System;

namespace BsaBrowser.Wildcard
{
    [Flags]
    public enum WildcardOptions
    {
        None = 0,
        Compiled = 1,
        IgnoreCase = 2,
        CultureInvariant = 4
    }
}

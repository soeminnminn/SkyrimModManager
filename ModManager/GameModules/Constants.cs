using System;

namespace ModManager.GameModules
{
    public enum GameID : int
    {
        UnKnown = 0,
        Morrowind,
        Oblivion,
        Skyrim,
        Fallout3,
        FalloutNV,
        Fallout4,
        SkyrimSE,
        Fallout4VR,
        SkyrimVR,
    }

    public enum HeaderFlag
    {
        Master = 0x1,
        Localized = 0x80,
        LightMaster = 0x200
    }

}
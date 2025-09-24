using System;

namespace ModManager.Plugin
{
    public interface IPluginParameter
    {
        object ParsePluginArguments(PluginCommand command, string[] args);
    }
}

using Dalamud.Configuration;
using SamplePlugin;
using System;

namespace SamplePlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool IsConfigWindowMovable { get; set; } = true;
        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome
        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}

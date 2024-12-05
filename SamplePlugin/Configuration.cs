using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace PeepingTim;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public bool SoundEnabled { get; set; } = false; // user configerable
    public float SoundVolume { get; set; } = 0.5f;

    public Vector4 targetingColor { get; set; } = new Vector4(0.0431f, 0.9569f, 0.1804f, 1.0000f);// user configerable
    public Vector4 unloadedColor { get; set; } = new Vector4(0.5f, 0.5f, 0.5f, 1f);// user configerable
    public Vector4 loadedColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);// user configerable

    public string DevVersion = "1.0.0.0";

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

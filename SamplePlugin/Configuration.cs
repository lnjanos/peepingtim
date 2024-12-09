using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace PeepingTim;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;
    public bool SoundEnabled { get; set; } = false;
    public bool SoundEnabledWindowClosed { get; set; } = false;
    public float SoundVolume { get; set; } = 0.5f;
    public static string OriginalSoundFile => Path.Combine(BasePath, "assets", "alert.wav");
    public string SoundFilePath { get; set; } = OriginalSoundFile;
    public bool StartOnStartup { get; set; } = false;

    public Vector4 titleColor { get; set; } = new Vector4(0.6f, 0.8f, 1.0f, 1.0f);
    public Vector4 targetingColor { get; set; } = new Vector4(0.0431f, 0.9569f, 0.1804f, 1.0000f);
    public Vector4 unloadedColor { get; set; } = new Vector4(0.5f, 0.5f, 0.5f, 1f);
    public Vector4 loadedColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

    public readonly string DevVersion = "1.0.1.3";

    public static bool LOCALCODING = false;
    public static string BasePath = LOCALCODING ? AppContext.BaseDirectory : Plugin.PluginInterface.AssemblyLocation.DirectoryName!;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}

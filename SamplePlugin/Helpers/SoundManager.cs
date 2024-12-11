using NAudio.Wave;
using PeepingTim.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PeepingTim.Helpers
{
    internal class SoundManager
    {

        private Plugin Plugin;

        public SoundManager(Plugin plugin) {
            this.Plugin = plugin;
        }

        public void CheckSoundFile()
        {
            var currentPath = this.Plugin.Configuration.SoundFilePath;
            var defaultPath = Configuration.OriginalSoundFile;

            if (!File.Exists(currentPath))
            {

                if (!File.Exists(defaultPath))
                {
                    var assemblyDir = Path.GetDirectoryName(Plugin.PluginInterface.AssemblyLocation.FullName) ?? "";
                    var assemblyDefaultPath = Path.Combine(assemblyDir, "assets", "alert.wav");

                    if (File.Exists(assemblyDefaultPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath)!);
                        File.Copy(assemblyDefaultPath, defaultPath, true);
                    }
                    else
                    {
                        Plugin.ChatGui.PrintError($"Default sound file not found at {assemblyDefaultPath}.");
                        return;
                    }
                }

                Plugin.Configuration.SoundFilePath = defaultPath;
                Plugin.Configuration.Save();
            }
        }

        public string CopySoundFileToPluginDirectory(string sourcePath)
        {
            var pluginDirectory = Configuration.BasePath;
            var assetsDirectory = Path.Combine(pluginDirectory, "assets");

            if (!Directory.Exists(assetsDirectory))
            {
                Directory.CreateDirectory(assetsDirectory);
            }

            // Dann wie gehabt:
            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(assetsDirectory, fileName);
            File.Copy(sourcePath, destinationPath, true);
            return destinationPath;
        }

        public void PlaySound(bool playAnyway = false)
        {
            if (!playAnyway && !Plugin.Configuration.SoundEnabledWindowClosed && !Plugin.IsMainWindowOpen()) return;

            new Thread(() =>
            {
                try
                {
                    string soundFilePath = Plugin.Configuration.SoundFilePath;

                    if (!File.Exists(soundFilePath))
                    {
                        Plugin.ChatGui.PrintError("Didn't find: " + soundFilePath);
                        return;
                    }

                    // Erkennen des Dateiformats und Laden des entsprechenden Readers
                    WaveStream? reader = GetAudioReader(soundFilePath);

                    if (reader == null)
                    {
                        Plugin.ChatGui.PrintError($"Unsupported file format: {Path.GetExtension(soundFilePath).ToLowerInvariant()}");
                        return;
                    }

                    using var channel = new WaveChannel32(reader)
                    {
                        Volume = Plugin.Configuration.SoundVolume,
                        PadWithZeroes = false
                    };

                    using (reader)
                    {
                        using var output = new WaveOutEvent();

                        try
                        {
                            output.Init(channel);
                            output.Play();

                            while (output.PlaybackState == PlaybackState.Playing)
                            {
                                Thread.Sleep(100);
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.ChatGui.PrintError($"Error playing sound: {ex}");
                        }
                    }
                }
                catch (Exception e)
                {
                    Plugin.ChatGui.PrintError($"Error initializing sound: {e}");
                }
            }).Start();
        }

        private WaveStream? GetAudioReader(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".wav" => new WaveFileReader(filePath),
                ".mp3" => new Mp3FileReader(filePath),
                ".aif" or ".aiff" => new AiffFileReader(filePath),
                ".wma" => new MediaFoundationReader(filePath), // WMA mit MediaFoundationReader
                ".aac" => new MediaFoundationReader(filePath), // AAC mit MediaFoundationReader
                _ => null
            };
        }

    }
}

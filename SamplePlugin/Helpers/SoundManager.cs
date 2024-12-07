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
        public string CopySoundFileToPluginDirectory(string sourcePath)
        {
            var pluginDirectory = Path.Combine(Plugin.PluginInterface.AssemblyLocation.DirectoryName!, "assets");

            if (!Directory.Exists(pluginDirectory))
            {
                Directory.CreateDirectory(pluginDirectory);
            }

            if (!Path.Exists(sourcePath)) return "";
            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(pluginDirectory, fileName);

            File.Copy(sourcePath, destinationPath, true);

            return destinationPath;
        }

        public void PlaySound()
        {
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

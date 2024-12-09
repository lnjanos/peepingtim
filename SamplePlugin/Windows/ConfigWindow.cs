using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PeepingTim.Helpers;

namespace PeepingTim.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;
        private Plugin Plugin;
        private FileDialogManager FileDialogManager;
        private SoundManager SoundManager;

        public ConfigWindow(Plugin plugin) : base("Peeping Tim Settings")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(450, 400);
            SizeCondition = ImGuiCond.FirstUseEver;
            FileDialogManager = new FileDialogManager();
            SoundManager = new SoundManager(plugin);

            this.Plugin = plugin;
            this.Configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public override void PreDraw()
        {
            if (Configuration.IsConfigWindowMovable)
            {
                Flags &= ~ImGuiWindowFlags.NoMove;
            }
            else
            {
                Flags |= ImGuiWindowFlags.NoMove;
            }
        }

        public override void Draw()
        {
            FileDialogManager.Draw();

            // General padding for a cleaner layout
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(15, 15));

            if (ImGui.BeginTabBar("##ConfigTabs"))
            {
                // ----- GENERAL TAB -----
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), "General Settings");
                    ImGui.Separator();
                    ImGui.Spacing();

                    var startupstart = Configuration.StartOnStartup;
                    if (ImGui.Checkbox("Start plugin on game startup", ref startupstart))
                    {
                        Configuration.StartOnStartup = startupstart;
                        Configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("If enabled, the plugin will automatically start when you launch the game.");
                    }

                    ImGui.EndTabItem();
                }

                // ----- SOUND TAB -----
                if (ImGui.BeginTabItem("Sound"))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1.0f), "Sound Settings");
                    ImGui.Separator();
                    ImGui.Spacing();

                    var soundEnabled = Configuration.SoundEnabled;
                    if (ImGui.Checkbox("Enable Sound", ref soundEnabled))
                    {
                        Configuration.SoundEnabled = soundEnabled;
                        Configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Enable or disable the plugin’s sound playback.");
                    }

                    // Show other sound options only if sound is enabled
                    if (Configuration.SoundEnabled)
                    {
                        ImGui.Indent();
                        ImGui.Spacing();

                        float volume = Configuration.SoundVolume;
                        if (ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f, "Volume: %.2f"))
                        {
                            Configuration.SoundVolume = volume;
                            Configuration.Save();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Adjust the volume for sound effects.");
                        }

                        if (ImGui.Button("Test Sound"))
                        {
                            SoundManager.PlaySound(true);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Click to play a test sound with the current settings.");
                        }

                        ImGui.Spacing();
                        var soundwithoutwindow = Configuration.SoundEnabledWindowClosed;
                        if (ImGui.Checkbox("Play sound when window is closed", ref soundwithoutwindow))
                        {
                            Configuration.SoundEnabledWindowClosed = soundwithoutwindow;
                            Configuration.Save();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("If enabled, sound will still play even if the main window is not visible.");
                        }

                        ImGui.Spacing();
                        ImGui.TextUnformatted("Path to audio file:");
                        Vector2 buttonSize;
                        ImGui.PushFont(UiBuilder.IconFont);
                        try
                        {
                            buttonSize = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Folder.ToIconString());
                        }
                        finally
                        {
                            ImGui.PopFont();
                        }

                        var path = Configuration.SoundFilePath ?? "";
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - buttonSize.X - 10);
                        if (ImGui.InputText("###sound-path", ref path, 1_000))
                        {
                            path = path.Trim();
                            bool isPath = path.Length == 0;
                            if (!isPath)
                            {
                                string newpath = SoundManager.CopySoundFileToPluginDirectory(path);
                                if (newpath != "") Configuration.SoundFilePath = newpath;
                            }
                            else
                            {
                                Configuration.SoundFilePath = Configuration.OriginalSoundFile;
                            }
                            Configuration.Save();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Specify the path to your preferred audio file.");
                        }

                        ImGui.SameLine();

                        ImGui.PushFont(UiBuilder.IconFont);
                        try
                        {
                            if (ImGui.Button(FontAwesomeIcon.Folder.ToIconString()))
                            {
                                FileDialogManager.OpenFileDialog(
                                    "Path to audio file",
                                    ".wav,.mp3,.aif,.aiff,.wma,.aac",
                                    (selected, selectedPath) =>
                                    {
                                        if (!selected) return;

                                        path = selectedPath.Trim();
                                        bool isPath = path.Length == 0;

                                        if (!isPath)
                                        {
                                            string newpath = SoundManager.CopySoundFileToPluginDirectory(path);
                                            if (newpath != "") Configuration.SoundFilePath = newpath;
                                        }
                                        else
                                        {
                                            Configuration.SoundFilePath = Configuration.OriginalSoundFile;
                                        }
                                        Configuration.Save();
                                    }
                                );
                            }
                        }
                        finally
                        {
                            ImGui.PopFont();
                        }

                        if (ImGui.Button("Reset to Default Soundfile"))
                        {
                            Configuration.SoundFilePath = Configuration.OriginalSoundFile;
                            Configuration.Save();
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Restores the default sound file that shipped with the plugin.");
                        }

                        ImGui.Unindent();
                    }

                    ImGui.EndTabItem();
                }

                // ----- COLORS TAB -----
                if (ImGui.BeginTabItem("Colors"))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(0.7f, 1.0f, 0.7f, 1.0f), "Color Customization");
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.Text("Adjust the colors for different states:");
                    ImGui.Spacing();

                    var colorPickerFlags = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.NoInputs;

                    // Active Color
                    var targetingColor = Configuration.targetingColor;
                    if (ImGui.ColorEdit4("Active", ref targetingColor, colorPickerFlags))
                    {
                        Configuration.targetingColor = targetingColor;
                        Configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Color used when the target is peeping.");
                    }

                    // Recent Color
                    var loadedColor = Configuration.loadedColor;
                    if (ImGui.ColorEdit4("Recent", ref loadedColor, colorPickerFlags))
                    {
                        Configuration.loadedColor = loadedColor;
                        Configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Color used for recently loaded/peeping targets.");
                    }

                    // Away Color
                    var unloadedColor = Configuration.unloadedColor;
                    if (ImGui.ColorEdit4("Away", ref unloadedColor, colorPickerFlags))
                    {
                        Configuration.unloadedColor = unloadedColor;
                        Configuration.Save();
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Color used for Peeper that are currently not loaded.");
                    }

                    ImGui.EndTabItem();
                }

                // ----- ABOUT TAB -----
                if (ImGui.BeginTabItem("About"))
                {
                    ImGui.Spacing();
                    ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.8f, 1.0f), "About Peeping Tim");
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.Text("Peeping Tim");
                    ImGui.Spacing();
                    ImGui.Text($"Version: {Configuration.DevVersion}");
                    ImGui.Text("Author: kcuY");
                    ImGui.Text("Discord (for feedback or issues): _yuck");
                    ImGui.Spacing();
                    ImGui.TextWrapped("Description: A slightly different version of Peeping Tom, offering additional functions.");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.PopStyleVar();
        }
    }
}

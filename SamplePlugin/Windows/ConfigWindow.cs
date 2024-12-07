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
        private FileDialogManager filedialog;
        private SoundManager SoundManager;

        public ConfigWindow(Plugin plugin) : base("Peeping Tim Settings")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(400, 350);
            SizeCondition = ImGuiCond.FirstUseEver;
            filedialog = new FileDialogManager();
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
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(15, 15));

            if (ImGui.BeginTabBar("##ConfigTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    ImGui.Text("General Settings");
                    ImGui.Separator();

                    var soundEnabled = Configuration.SoundEnabled;
                    if (ImGui.Checkbox("Enable Sound", ref soundEnabled))
                    {
                        Configuration.SoundEnabled = soundEnabled;
                        Configuration.Save();
                    }

                    if (Configuration.SoundEnabled)
                    {
                        float volume = Configuration.SoundVolume;
                        if (ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f, "Volume: %.2f"))
                        {
                            Configuration.SoundVolume = volume;
                            Configuration.Save();
                        }

                        if (ImGui.Button("Test Sound"))
                        {
                            SoundManager.PlaySound();
                        }
                    }

                    if (Configuration.SoundEnabled)
                    {
                        bool soundwithoutwindow = Configuration.SoundEnabledWindowClosed;
                        if (ImGui.Checkbox("Play sound when window is closed", ref soundwithoutwindow)) {
                            Configuration.SoundEnabledWindowClosed = soundEnabled;
                            Configuration.Save();
                        }
                    }

                    if (Configuration.SoundEnabled)
                    {
                        ImGui.TextUnformatted("Path to audio file");
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
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - buttonSize.X);
                        if (ImGui.InputText("###sound-path", ref path, 1_000))
                        {
                            path = path.Trim();
                            bool isPath = path.Length == 0;

                            if (!isPath)
                            {
                                string newpath = SoundManager.CopySoundFileToPluginDirectory(path);
                                if ( newpath != "" ) Configuration.SoundFilePath = newpath;
                            } else
                            {
                                Configuration.SoundFilePath = Configuration.OriginalSoundFile;
                            }
                            Configuration.Save();
                        }

                        ImGui.SameLine();

                        ImGui.PushFont(UiBuilder.IconFont);
                        try
                        {
                            if (ImGui.Button(FontAwesomeIcon.Folder.ToIconString()))
                            {
                                filedialog.OpenFileDialog(
                                    "Path to audio file",
                                    ".wav,.mp3,.aif,.aiff,.wma,.aac",
                                    (selected, selectedPath) => {
                                        if (!selected)
                                        {
                                            return;
                                        }

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


                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Colors"))
                {
                    ImGui.Text("Customize Colors");
                    ImGui.Separator();

                    var colorPickerFlags = ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.NoInputs;

                    // Active Color
                    var targetingColor = Configuration.targetingColor;
                    if (ImGui.ColorEdit4("Active", ref targetingColor, colorPickerFlags))
                    {
                        Configuration.targetingColor = targetingColor;
                        Configuration.Save();
                    }

                    // Recent Color
                    var loadedColor = Configuration.loadedColor;
                    if (ImGui.ColorEdit4("Recent", ref loadedColor, colorPickerFlags))
                    {
                        Configuration.loadedColor = loadedColor;
                        Configuration.Save();
                    }

                    // Away Color
                    var unloadedColor = Configuration.unloadedColor;
                    if (ImGui.ColorEdit4("Away", ref unloadedColor, colorPickerFlags))
                    {
                        Configuration.unloadedColor = unloadedColor;
                        Configuration.Save();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("About"))
                {
                    ImGui.Text("Peeping Tim");
                    ImGui.Separator();
                    ImGui.Text($"Version: {Configuration.DevVersion}");
                    ImGui.Text("Author: kcuY");
                    ImGui.TextWrapped("Description: Just a different Version of Peeping Tom.");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.PopStyleVar();
        }
    }
}

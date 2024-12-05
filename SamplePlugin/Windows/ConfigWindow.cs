using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace PeepingTim.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;
        private Plugin Plugin; // Referenz auf das Plugin

        public ConfigWindow(Plugin plugin) : base("Peeping Tim Settings")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            Size = new Vector2(400, 350);
            SizeCondition = ImGuiCond.FirstUseEver;

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

                    // Enable Sound
                    var soundEnabled = Configuration.SoundEnabled;
                    if (ImGui.Checkbox("Enable Sound", ref soundEnabled))
                    {
                        Configuration.SoundEnabled = soundEnabled;
                        Configuration.Save();
                    }

                    // Wenn Sound aktiviert ist, zeige Slider und Test-Button
                    if (Configuration.SoundEnabled)
                    {
                        // Volume Slider
                        float volume = Configuration.SoundVolume;
                        if (ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f, "Volume: %.2f"))
                        {
                            Configuration.SoundVolume = volume;
                            Configuration.Save();
                        }

                        // Test Sound Button
                        if (ImGui.Button("Test Sound"))
                        {
                            // Ruft die Plugin-Methode auf, um den Sound abzuspielen
                            Plugin.PlaySound();
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

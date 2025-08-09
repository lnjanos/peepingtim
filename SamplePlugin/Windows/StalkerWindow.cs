using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.GameFunctions;
using Dalamud.Game.ClientState.Objects.Types;

namespace PeepingTim.Windows
{
    public class StalkerWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private Plugin.ViewerInfo user; // Der Stalker selbst

        public StalkerWindow(Plugin plugin, Plugin.ViewerInfo user)
            : base(user.Name, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(225, 225),
                MaximumSize = new Vector2(225, 300)
            };

            Size = new Vector2(225, 225);
            SizeCondition = ImGuiCond.FirstUseEver;

            TitleBarButtons.Add(Support.NavBarBtn);

            this.Plugin = plugin;
            this.user = user;
            this.IsOpen = true;
        }

        public void Dispose()
        {
            // Dispose resources if needed
        }

        public override void OnClose()
        {
            this.Plugin.CloseStalkerWindow(this.user);
            base.OnClose();
        }

        public override void Draw()
        {
            // 1) Wen schaut der Stalker an?
            var lookinAt = new List<Plugin.ViewerInfo?>()
            {
                this.Plugin.GetStalkerTarget(user)
            };

            var allViewers = Plugin.GetAllViewers();

            ImGui.Spacing();
            ImGui.TextColored(Plugin.Configuration.titleColor, "Targeting");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted("Left click: Target this character\nRight click: Open context menu (e.g., send a tell or view adventure plate)");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.Separator();
            ImGui.Spacing();

            if (lookinAt[0] != null)
            {
                foreach (var viewer in lookinAt)
                {
                    // Farbe
                    if (viewer.IsActive)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    else if (!viewer.isLoaded)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);

                    var time = viewer.LastSeen.ToString("HH:mm");
                    bool isSelected = false;
                    if (ImGui.Selectable(viewer.Name, isSelected))
                    {
                        // ...
                    }

                    float windowWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        if (viewer.isLoaded && !viewer.isFocused)
                        {
                            foreach (var v in allViewers)
                            {
                                if (v.isFocused) Plugin.HighlightCharacter(v);
                            }
                            Plugin.HighlightCharacter(viewer);
                        }

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            Plugin.TargetCharacter(viewer);
                            ImGui.SetWindowFocus(ImU8String.Empty);
                        }
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"ContextMenu_{viewer.Name}");
                        }
                    }
                    else if (viewer.isFocused)
                    {
                        Plugin.HighlightCharacter(viewer);
                    }

                    Popup.Draw(Plugin, viewer);

                    // Timestamp
                    if (viewer.IsActive)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    else if (!viewer.isLoaded)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);

                    ImGui.SameLine(windowWidth - ImGui.CalcTextSize(time).X);
                    ImGui.TextUnformatted(time);
                    ImGui.PopStyleColor();
                }
            }
            else
            {
                ImGui.Text("Is not focusing anyone.");
            }

            float availableHeight = ImGui.GetContentRegionAvail().Y;


            // 2) Wer schaut den Stalker an?
            ImGui.Spacing();
            ImGui.TextColored(Plugin.Configuration.titleColor, "Current Viewers");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted("Left click: Target this character\nRight click: Open context menu (e.g., send a tell or view adventure plate)");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.Separator();
            ImGui.Spacing();

            // Gleiche Child-Region wie oben, 
            // oder Du splittest Dein Fenster in 2 Childs etc.

            float availableHeight2 = ImGui.GetContentRegionAvail().Y;
            ImGui.BeginChild("ViewerListChild", new Vector2(0, availableHeight2), false, ImGuiWindowFlags.HorizontalScrollbar);

            var stalkerViewers = Plugin.GetStalkerViewers(user);
            if (stalkerViewers.Count > 0)
            {
                foreach (var viewer in stalkerViewers)
                {
                    if (viewer.IsActive)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    else if (!viewer.isLoaded)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);

                    var time = viewer.LastSeen.ToString("HH:mm");
                    bool isSelected = false;
                    if (ImGui.Selectable(viewer.Name, isSelected))
                    {
                    }

                    float windowWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
                    ImGui.PopStyleColor();

                    if (ImGui.IsItemHovered())
                    {
                        if (viewer.isLoaded && !viewer.isFocused)
                        {
                            foreach (var v in allViewers)
                            {
                                if (v.isFocused) Plugin.HighlightCharacter(v);
                            }
                            Plugin.HighlightCharacter(viewer);
                        }

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            Plugin.TargetCharacter(viewer);
                            ImGui.SetWindowFocus(ImU8String.Empty);
                        }

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"ContextMenu_{viewer.Name}");
                        }
                    }
                    else if (viewer.isFocused)
                    {
                        Plugin.HighlightCharacter(viewer);
                    }

                    Popup.Draw(Plugin, viewer);

                    if (viewer.IsActive)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    else if (!viewer.isLoaded)
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    else
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);

                    ImGui.SameLine(windowWidth - ImGui.CalcTextSize(time).X);
                    ImGui.TextUnformatted(time);
                    ImGui.PopStyleColor();
                }
            }
            else
            {
                ImGui.Text("No viewers yet.");
            }

            ImGui.EndChild();
        }
    }
}

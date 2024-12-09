using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ECommons.GameFunctions;
using ECommons.EzEventManager;
using ECommons.PartyFunctions;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Xml.Linq;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using static System.Net.Mime.MediaTypeNames;
using Dalamud.IoC;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PeepingTim.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;

        public MainWindow(Plugin plugin) : base(
            "Peeping Tim",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(225, 130),
                MaximumSize = new Vector2(225, 300)
            };

            Size = new Vector2(225, 130);
            SizeCondition = ImGuiCond.FirstUseEver;

            this.Plugin = plugin;
        }

        public void Dispose()
        {
            // Dispose resources if needed
        }

        public override void Draw()
        {
            var viewers = Plugin.GetViewers();

            // Title Section
            ImGui.Spacing();
            ImGui.TextColored(Plugin.Configuration.titleColor, "Current Viewers");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                // Tooltip explaining left/right click behavior
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted("Left click: Target this character\nRight click: Open context menu (e.g., send a tell or view adventure plate)");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
            ImGui.Separator();
            ImGui.Spacing();


            // Viewer List Area
            //ImGui.BeginChild("UserList", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), true, ImGuiWindowFlags.HorizontalScrollbar);
            if (viewers.Count > 0)
            {
                // Iterate through each viewer and display with color-coding and context menu
                foreach (var viewer in viewers)
                {
                    // Determine color based on viewer state
                    if (viewer.IsActive)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    }
                    else if (!viewer.isLoaded)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);
                    }

                    var time = viewer.LastSeen.ToString("HH:mm");
                    ImGui.Selectable(viewer.Name, false);
                    var windowWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
                    ImGui.PopStyleColor();

                    // Hover and click events
                    if (ImGui.IsItemHovered())
                    {
                        // Highlight focused viewer if applicable
                        if (viewer.isLoaded && !viewer.isFocused)
                        {
                            // Unhighlight previously focused viewers, highlight the current hovered one
                            foreach (var v in viewers)
                            {
                                if (v.isFocused)
                                {
                                    Plugin.HighlightCharacter(v);
                                }
                            }
                            Plugin.HighlightCharacter(viewer);
                        }

                        // Left click: target the character
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            Plugin.TargetCharacter(viewer);
                            ImGui.SetWindowFocus(null);
                        }

                        // Right click: open context menu
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"ContextMenu_{viewer.Name}");
                        }
                    }
                    else if (viewer.isFocused)
                    {
                        // Keep highlighting the focused character if mouse is not hovered anymore
                        Plugin.HighlightCharacter(viewer);
                    }

                    // Context Menu for each viewer
                    if (ImGui.BeginPopup($"ContextMenu_{viewer.Name}"))
                    {
                        if (ImGui.MenuItem("Send Tell"))
                        {
                            Plugin.OpenMessageWindow(viewer);
                        }

                        // Adventure Plate option if viewer is loaded
                        if (viewer.isLoaded)
                        {
                            if (ImGui.MenuItem("View Adventure Plate"))
                            {
                                foreach (var x in Svc.Objects)
                                {
                                    if (x is IPlayerCharacter pc &&
                                        pc.Name.ToString() == viewer.Name &&
                                        Plugin.GetWorldName(pc.HomeWorld.RowId) == viewer.World)
                                    {
                                        unsafe
                                        {
                                            GameObject* xStruct = x.Struct();
                                            AgentCharaCard.Instance()->OpenCharaCard(xStruct);
                                        }
                                    }
                                }
                            }
                        }
                        ImGui.EndPopup();
                    }

                    // Re-apply color for the timestamp line
                    if (viewer.IsActive)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.targetingColor);
                    }
                    else if (!viewer.isLoaded)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.unloadedColor);
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Plugin.Configuration.loadedColor);
                    }

                    // Display the timestamp on the same line, right-aligned
                    ImGui.SameLine(windowWidth - ImGui.CalcTextSize(time).X);
                    ImGui.TextUnformatted(time);
                    ImGui.PopStyleColor();
                }
            }
            else
            {
                ImGui.Text("No viewers yet.");
            }
        }
    }
}

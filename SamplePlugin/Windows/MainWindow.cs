using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using static System.Net.Mime.MediaTypeNames;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ECommons.GameFunctions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Network.Structures.InfoProxy;
using ECommons;
using Dalamud.DrunkenToad.Extensions;

namespace PeepingTim.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;

        public MainWindow(Plugin plugin)
            : base("Peeping Tim", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
            var allViewers = Plugin.GetAllViewers();
            var viewers = Plugin.GetViewers();

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

            float availableHeight = ImGui.GetContentRegionAvail().Y;
            ImGui.BeginChild("ViewerListChild", new Vector2(0, availableHeight), false, ImGuiWindowFlags.HorizontalScrollbar);

            if (viewers.Count > 0)
            {
                foreach (var viewer in viewers)
                {
                    // Farbe je nach Status
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
                        // Falls du Selektion brauchst
                    }

                    float windowWidth = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
                    ImGui.PopStyleColor();

                    // Hover
                    if (ImGui.IsItemHovered())
                    {
                        // Highlight, wenn isLoaded und noch nicht isFocused
                        if (viewer.isLoaded && !viewer.isFocused)
                        {
                            // Alle ent-Highlighten?
                            // (Falls du nur einen auf einmal highlighten willst)
                            foreach (var v in allViewers)
                            {
                                if (v.isFocused) Plugin.HighlightCharacter(v);
                            }
                            Plugin.HighlightCharacter(viewer);
                        }

                        // Linksklick = Target
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            Plugin.TargetCharacter(viewer);
                            ImGui.SetWindowFocus(null);
                        }

                        // Rechtsklick => Kontextmenü
                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"ContextMenu_{viewer.Name}");
                        }
                    }
                    else if (viewer.isFocused)
                    {
                        // Falls du möchtest, dass der Fokus aufgehoben wird,
                        // wenn die Maus weg geht, kannst du es hier toggeln.
                        // Hier bleibt es der Demo halber so, wie es ist.
                        Plugin.HighlightCharacter(viewer);
                    }

                    // Kontextmenü
                    if (ImGui.BeginPopup($"ContextMenu_{viewer.Name}"))
                    {
                        if (ImGui.MenuItem("Send Tell"))
                        {
                            Plugin.OpenMessageWindow(viewer);
                        }

                        if (viewer.isLoaded)
                        {
                            if (ImGui.MenuItem("View Adventure Plate"))
                            {
                                IGameObject? pc = Svc.Objects.SearchById(viewer.lastKnownGameObjectId);
                                if (pc != null)
                                {
                                    unsafe
                                    {
                                        Svc.Framework.RunOnTick(() =>
                                        {
                                            AgentCharaCard.Instance()->OpenCharaCard(pc.Struct());
                                        });
                                    }
                                }
                            }
                            if (ImGui.MenuItem("Examine"))
                            {
                                IGameObject? pc = Svc.Objects.SearchById(viewer.lastKnownGameObjectId);
                                if (pc != null)
                                {
                                    unsafe
                                    {
                                        Svc.Framework.RunOnTick(() =>
                                        {
                                            AgentInspect.Instance()->ExamineCharacter(pc.EntityId);
                                        });
                                    }
                                }
                            }
                            if (ImGui.MenuItem("Stalk"))
                            {
                                Plugin.OpenStalkWindow(viewer);
                            }
                        }
                        ImGui.EndPopup();
                    }

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
                ImGui.Text("No viewers yet.");
            }

            ImGui.EndChild();
        }
    }
}

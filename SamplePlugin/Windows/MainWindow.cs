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
                MinimumSize = new Vector2(250, 150),
                MaximumSize = new Vector2(250, 800)
            };

            Size = new Vector2(250, 200);
            SizeCondition = ImGuiCond.FirstUseEver;


            this.Plugin = plugin;
        }

        public void Dispose()
        {
            // Ressourcenfreigabe, falls nötig
        }

        public override void Draw()
        {
            var viewers = Plugin.GetViewers();

            ImGui.Text("Peeper:");
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted("Leftclick -> Target | Rightclick -> Functions");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.BeginChild("UserList", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), true, ImGuiWindowFlags.HorizontalScrollbar);
            if (viewers.Count > 0)
            {
                foreach (var viewer in viewers)
                {

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

                    // Handle hover and click events
                    if (ImGui.IsItemHovered())
                    {
                        if (viewer.isLoaded && !viewer.isFocused)
                        {
                            foreach (var v in viewers)
                            {
                                if (v.isFocused)
                                {
                                    Plugin.HighlightCharacter(v);
                                }
                            }
                            Plugin.HighlightCharacter(viewer);
                        }

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            Plugin.TargetCharacter(viewer);
                            ImGui.SetWindowFocus(null);
                        }

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"ContextMenu_{viewer.Name}");
                        }
                    }
                    else if (viewer.isFocused)
                    {
                        Plugin.HighlightCharacter(viewer);
                    }

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
                                foreach (var x in Svc.Objects)
                                {
                                    if (x is IPlayerCharacter pc && pc.Name.ToString() == viewer.Name && Plugin.GetWorldName(pc.HomeWorld.RowId) == viewer.World)
                                    {
                                        unsafe
                                        {
                                            GameObject* xStruct = x.Struct();
                                            AgentCharaCard.Instance()->OpenCharaCard(xStruct);
                                            
                                        }
                                        PluginLog.Debug($"Opening characard via gameobject {x}");
                                    }
                                }
                            }
                        }
                        ImGui.EndPopup();
                    }

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

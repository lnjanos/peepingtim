using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using ECommons.EzSharedDataManager;
using ECommons.ImGuiMethods;
using ECommons;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkTooltipManager.Delegates;
using System.Drawing;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ECommons.GameFunctions;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace PeepingTim.Windows
{
    public static class Popup
    {
       
        public static void Draw(Plugin Plugin, Plugin.ViewerInfo viewer)
        {
            if (ImGui.BeginPopup($"ContextMenu_{viewer.Name}"))
            {
                if (Plugin.Configuration.TellOption && ImGui.MenuItem("Send Tell"))
                {
                    Plugin.OpenMessageWindow(viewer);
                }

                if (Plugin.Configuration.DoteOption && viewer.isLoaded)
                {
                    if (ImGui.MenuItem("Dote"))
                    {
                        Plugin.DoteViewer(viewer);
                    }
                }

                if (Plugin.Configuration.AdventurePlateOption && viewer.cid != 0)
                {
                    if (ImGui.MenuItem("View Adventure Plate"))
                    {
                        unsafe
                        {
                            Svc.Framework.RunOnTick(() =>
                            {
                                AgentCharaCard.Instance()->OpenCharaCard(viewer.cid);
                            });
                        }
                    }
                }
                else if (Plugin.Configuration.AdventurePlateOption && viewer.isLoaded)
                {
                    if (ImGui.MenuItem("View Adventure Plate"))
                    {
                        IGameObject? pc = Svc.Objects.SearchById(viewer.lastKnownGameObjectId);
                        IPlayerCharacter? x = pc as IPlayerCharacter;
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
                }

                if (viewer.isLoaded)
                {
                    if (Plugin.Configuration.ExamineOption && ImGui.MenuItem("Examine"))
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
                    if (Plugin.Configuration.StalkOption && ImGui.MenuItem("Stalk"))
                    {
                        Plugin.OpenStalkWindow(viewer);
                    }
                    if (Plugin.Configuration.SearchInfoOption && ImGui.MenuItem("Open Context Menu"))
                    {
                        IGameObject? pc = Svc.Objects.SearchById(viewer.lastKnownGameObjectId);
                        if (pc != null)
                        {
                            unsafe
                            {
                                Svc.Framework.RunOnTick(() =>
                                {
                                    AgentHUD.Instance()->OpenContextMenuFromTarget(pc.Struct());
                                });
                            }
                        }
                    }
                }
                ImGui.EndPopup();
            }
        }

    }
}

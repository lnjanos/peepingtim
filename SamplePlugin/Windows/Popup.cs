using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace PeepingTim.Windows
{
    public static class Popup
    {
        private static bool OpenBuffModal;
        private static string BuffOwner = string.Empty;
        private static List<MoodlesStatusInfo> Buffs = new();

        private static int LastBuffModalDrawFrame = -1;

        private static readonly Regex MoodlesTagRegex =
            new(@"\[/?(?:color|glow|i|b|u|s)(?:=[^\]]+)?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string CleanMoodlesText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return MoodlesTagRegex
                .Replace(text, string.Empty)
                .Replace("&", string.Empty)
                .Trim();
        }

        public static void Draw(Plugin Plugin, Plugin.ViewerInfo viewer)
        {
            if (ImGui.BeginPopup($"ContextMenu_{viewer.Name}"))
            {
                if (Plugin.Configuration.TellOption && ImGui.MenuItem("Send Tell"))
                    Plugin.OpenMessageWindow(viewer);

                if (Plugin.Configuration.DoteOption && viewer.isLoaded && ImGui.MenuItem("Dote"))
                    Plugin.DoteViewer(viewer);

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

                    if (Plugin.Configuration.NoNoNo && ImGui.MenuItem("Stalk"))
                        Plugin.OpenStalkWindow(viewer);

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

                    DrawBuffMenuItem(Plugin, viewer);
                }

                ImGui.EndPopup();
            }

            DrawBuffModal(Plugin);
        }

        private static void DrawBuffMenuItem(Plugin Plugin, Plugin.ViewerInfo viewer)
        {
            if (!Plugin.Configuration.BuffInfoOption)
                return;

            if (!Plugin.HasMoodlesIpc())
                return;

            IGameObject? pc = Svc.Objects.SearchById(viewer.lastKnownGameObjectId);
            if (pc is not IPlayerCharacter player)
                return;

            List<MoodlesStatusInfo> buffs;

            try
            {
                buffs = Plugin.TryGetMoodlesStatus(player) ?? new List<MoodlesStatusInfo>();
            }
            catch
            {
                return;
            }

            if (buffs.Count == 0)
                return;

            if (ImGui.MenuItem($"View Buffs ({buffs.Count})"))
            {
                BuffOwner = viewer.Name;
                Buffs = buffs;
                OpenBuffModal = true;
            }
        }

        private static void DrawBuffModal(Plugin Plugin)
        {
            int frame = ImGui.GetFrameCount();

            if (LastBuffModalDrawFrame == frame)
                return;

            LastBuffModalDrawFrame = frame;

            if (OpenBuffModal)
            {
                ImGui.OpenPopup("Moodles Buffs");
                OpenBuffModal = false;
            }

            bool open = true;

            if (!ImGui.BeginPopupModal("Moodles Buffs", ref open, ImGuiWindowFlags.AlwaysAutoResize))
                return;

            ImGui.TextColored(Plugin.Configuration.titleColor, $"Buffs for {BuffOwner}");
            ImGui.Separator();
            ImGui.Spacing();

            if (Buffs.Count == 0)
            {
                ImGui.TextDisabled("No Moodles buffs found.");
            }
            else
            {
                float height = Math.Min(420f, Math.Max(110f, Buffs.Count * 105f));

                ImGui.BeginChild("MoodlesBuffList", new Vector2(560, height), true);

                for (int i = 0; i < Buffs.Count; i++)
                {
                    DrawBuffEntry(Buffs[i]);

                    if (i < Buffs.Count - 1)
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Spacing();
                    }
                }

                ImGui.EndChild();
            }

            ImGui.Spacing();

            if (ImGui.Button("Close", new Vector2(100, 0)))
            {
                Buffs = new List<MoodlesStatusInfo>();
                BuffOwner = string.Empty;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        private static void DrawBuffEntry(MoodlesStatusInfo buff)
        {
            string title = CleanMoodlesText(buff.Title);
            if (string.IsNullOrWhiteSpace(title))
                title = "(Untitled Moodle)";

            string description = CleanMoodlesText(buff.Description);

            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 520f);

            ImGui.TextColored(GetBuffTypeColor(buff.Type), title);

            if (buff.Stacks > 1)
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"x{buff.Stacks}");
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                ImGui.Spacing();
                ImGui.TextWrapped(description);
            }

            if (!string.IsNullOrWhiteSpace(buff.Applier))
                ImGui.TextDisabled($"Applied by: {CleanMoodlesText(buff.Applier)}");

            if (!string.IsNullOrWhiteSpace(buff.CustomVFXPath))
                ImGui.TextDisabled($"VFX: {buff.CustomVFXPath}");

            ImGui.PopTextWrapPos();
        }

        private static Vector4 GetBuffTypeColor(StatusType type)
        {
            return type switch
            {
                StatusType.Positive => new Vector4(0.45f, 0.9f, 0.45f, 1f),
                StatusType.Negative => new Vector4(1f, 0.35f, 0.35f, 1f),
                StatusType.Special => new Vector4(0.55f, 0.75f, 1f, 1f),
                _ => new Vector4(1f, 1f, 1f, 1f)
            };
        }
    }
}

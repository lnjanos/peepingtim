using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using System.Numerics;
using System.Linq;

namespace PeepingTim.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;

        private bool soundEnabled = true;

        public MainWindow(Plugin plugin) : base(
            "PeepingTim",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(250, 300),
                MaximumSize = new Vector2(600, 800)
            };

            this.Plugin = plugin;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            // Checkbox for sound notifications
            ImGui.Checkbox("Enable Sound Notifications", ref soundEnabled);
            Plugin.SoundEnabled = soundEnabled;

            var currentViewers = Plugin.GetCurrentViewers();
            var pastViewers = Plugin.GetPastViewers();
            var viewerTimestamps = Plugin.GetViewerTimestamps();

            ImGui.Text("Peeper:");

            if (pastViewers.Count > 0)
            {
                foreach (var viewer in pastViewers)
                {
                    bool isActive = currentViewers.Exists(v => v.GameObjectId == viewer.Key.GameObjectId);
                    bool isTargetable = viewer.Value;
                    var name = $"{viewer.Key.Name}";
                    var timestamp = viewerTimestamps.ContainsKey(viewer.Key.Name) ? viewerTimestamps[viewer.Key.Name].ToString("HH:mm") : "";

                    if (isActive)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0431f, 0.9569f, 0.1804f, 1.0000f)); // Weiß für aktive Betrachter
                    } else if (isTargetable)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f)); // Weiß für frühere Betrachter
                    } else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f)); // Grau für frühere nicht anvisierbare Betrachter
                    }

                    // Starten einer neuen Gruppe
                    ImGui.BeginGroup();

                    // Speichern der aktuellen Cursorposition
                    Vector2 cursorPos = ImGui.GetCursorScreenPos();

                    // Zeichnen des Namens
                    ImGui.Text(name);

                    // Berechnen der Größe des Zeitstempels
                    Vector2 timestampSize = ImGui.CalcTextSize(timestamp);

                    // Positionieren des Cursors für den Zeitstempel
                    float windowWidth = ImGui.GetWindowContentRegionMax().X + ImGui.GetWindowPos().X;
                    float timestampPosX = windowWidth - timestampSize.X - ImGui.GetStyle().ItemSpacing.X;
                    ImGui.SetCursorScreenPos(new Vector2(timestampPosX, cursorPos.Y));

                    // Zeichnen des Zeitstempels
                    ImGui.Text(timestamp);

                    // Beenden der Gruppe
                    ImGui.EndGroup();

                    // Erstellen eines unsichtbaren Buttons über die gesamte Zeile für Hover- und Klick-Ereignisse
                    Vector2 itemSize = new Vector2(windowWidth - cursorPos.X, ImGui.GetTextLineHeightWithSpacing());
                    ImGui.SetCursorScreenPos(cursorPos);
                    ImGui.InvisibleButton($"##viewer_{viewer.Key.GameObjectId}", itemSize);

                    // Interaktionen behandeln
                    if (ImGui.IsItemHovered())
                    {
                        Plugin.HighlightCharacter(viewer.Key);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        Plugin.TargetCharacter(viewer.Key);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup($"ContextMenu_{viewer.Key.GameObjectId}");
                    }

                    // Begin the context menu popup
                    if (ImGui.BeginPopup($"ContextMenu_{viewer.Key.GameObjectId}"))
                    {
                        if (ImGui.MenuItem("Send Tell"))
                        {
                            Plugin.SendChatCommand("tell", viewer.Key);
                        }
                        if (ImGui.MenuItem("View Adventurer Plate"))
                        {
                            Plugin.OpenAdventurerPlate(viewer.Key);
                        }
                        ImGui.EndPopup();
                    }

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

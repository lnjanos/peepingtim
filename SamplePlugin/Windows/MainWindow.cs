using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System.Xml.Linq;

namespace PeepingTim.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin Plugin;

        private bool soundEnabled = false;
        Dictionary<string, string> messageInputs = new Dictionary<string, string>();

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
            // Checkbox für Sound-Benachrichtigungen
            ImGui.Checkbox("Enable Sound Notifications", ref soundEnabled);
            Plugin.SoundEnabled = soundEnabled;

            var viewers = Plugin.GetViewers();

            ImGui.Button("Hey");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
                Plugin.OpenChatWith(
                    new Plugin.ViewerInfo
                    {
                        Name = "Liz Blackstone",
                        World = "Twintania",
                        IsActive = true,
                        isLoaded = true,
                        FirstSeen = DateTime.Now,
                        LastSeen = DateTime.Now
                    }, "heyo");
            }
            
            ImGui.Text("Peeper:");

            if (viewers.Count > 0)
            {
                foreach (var viewer in viewers)
                {
                    var name = $"{viewer.Name}@{viewer.World}";
                    var timestamp = viewer.LastSeen.ToString("HH:mm");

                    if (viewer.IsActive)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0431f, 0.9569f, 0.1804f, 1.0000f)); // Grün für aktive Betrachter
                    }
                    else if (!viewer.isLoaded)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f)); // Weiß für frühere Betrachter
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
                    ImGui.InvisibleButton($"##viewer_{name}", itemSize);

                    // Interaktionen behandeln
                    if (ImGui.IsItemHovered())
                    {
                        Plugin.HighlightCharacter(viewer);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        Plugin.TargetCharacter(viewer);
                        ImGui.SetWindowFocus(null);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup($"ContextMenu_{name}");
                    }

                    // Begin des Kontextmenüs
                    if (ImGui.BeginPopup($"ContextMenu_{name}"))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));
                        if (ImGui.MenuItem("Send Tell"))
                        {
                            Plugin.OpenMessageWindow(viewer);
                        }
                        if (viewer.isLoaded)
                        {
                            if (ImGui.MenuItem("View Adventure Plate"))
                            {
                                //Plugin.OpenAdventurePlate(viewer);
                            }
                        }
                        ImGui.EndPopup();
                        ImGui.PopStyleColor();
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

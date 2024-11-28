// MessageWindow.cs
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace PeepingTim.Windows
{
    public class MessageWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private string message = "";
        private Plugin.ViewerInfo Viewer;

        public MessageWindow(Plugin plugin, Plugin.ViewerInfo viewer) : base(
            $"Send Tell to {viewer.Name}@{viewer.World}",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.Plugin = plugin;
            this.Viewer = viewer;

            // Optional: Fenstergrößenbeschränkungen setzen
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 200),
                MaximumSize = new Vector2(600, 400)
            };

            // Fenster beim Erstellen öffnen
            this.IsOpen = true;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.Text($"Send a message to {Viewer.Name}@{Viewer.World}:");
            ImGui.InputTextMultiline("##messageInput", ref message, 1024, new Vector2(-1, 100));

            if (ImGui.Button("Send"))
            {
                if (!string.IsNullOrEmpty(message))
                {
                    Plugin.OpenChatWith(Viewer, message);
                    message = "";
                    this.IsOpen = false; // Fenster schließen
                }
                else
                {
                    Plugin.ChatGui.PrintError("Please enter a message before sending.");
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                this.IsOpen = false; // Fenster schließen
            }
        }
    }
}

using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace PeepingTim.Windows
{
    public class MessageWindow : Window, IDisposable
    {
        private Plugin Plugin;
        private string message = string.Empty;
        private Plugin.ViewerInfo Viewer;

        public MessageWindow(Plugin plugin, Plugin.ViewerInfo viewer)
            : base(
                // Fenster-Titel
                $"Send Tell to {viewer.Name}",

                // Flags: kein Scrollbar, kein Scrolling mit der Maus
                //        + automatische Fenstergröße an Content angepasst
                ImGuiWindowFlags.NoScrollbar
                | ImGuiWindowFlags.NoScrollWithMouse
                | ImGuiWindowFlags.AlwaysAutoResize
            )
        {
            this.Plugin = plugin;
            this.Viewer = viewer;

            TitleBarButtons.Add(Support.NavBarBtn);

            // Fenster direkt öffnen
            this.IsOpen = true;

            // (Optional) Minimalgröße für Komfort, aber oben lassen wir AutoResize das Meiste machen
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(250, 0),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose()
        {
            // Clean-up falls nötig
        }

        public override void Draw()
        {
            // Setze den Tastaturfokus auf das Eingabefeld,
            // wenn das Fenster frisch erscheint
            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetKeyboardFocusHere();
            }

            // Überschrift: "Send a quick tell to <Name>"
            ImGui.Text("Send a quick tell to:");
            ImGui.SameLine();
            ImGui.TextColored(Plugin.Configuration.titleColor, Viewer.Name);

            ImGui.Spacing();

            // Eingabefeld mit dezenter Hint
            // (-1) für volle Breite in diesem Fenster
            ImGui.PushItemWidth(-1);
            if (ImGui.InputTextWithHint(
                "##messageInput",    // Interner Name (Label)
                "Type your message...",
                ref message,
                1024,
                ImGuiInputTextFlags.EnterReturnsTrue))
            {
                // Aktion bei Enter
                AttemptSendMessage();
            }
            ImGui.PopItemWidth();

            ImGui.Spacing();

            // Buttons "Send" + "Cancel" nebeneinander
            if (ImGui.Button("Send"))
            {
                AttemptSendMessage();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                this.IsOpen = false; // Fenster schließen
            }
        }

        private void AttemptSendMessage()
        {
            if (!string.IsNullOrEmpty(message))
            {
                Plugin.OpenChatWith(Viewer, message);
                message = string.Empty;
                this.IsOpen = false; // Fenster schließen
            }
            else
            {
                Plugin.ChatGui.PrintError("Please enter a message before sending.");
            }
        }
    }
}

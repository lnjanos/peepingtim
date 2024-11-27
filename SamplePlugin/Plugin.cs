using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Collections.Generic;
using System;
using PeepingTim.Windows;
using ImGuiNET;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;

namespace PeepingTim;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "PeepingTim";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName1 = "/ptim";
    private const string CommandName2 = "/peepingtim";

    ECommonsMain.Init(pluginInterface, this);
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("PeepingTim");
    private MainWindow MainWindow { get; init; }

    // Dictionary to store viewer information
    private Dictionary<string, ViewerInfo> viewers = new();

    public bool SoundEnabled = false;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(MainWindow);

        // Add commands
        CommandManager.AddHandler(CommandName1, new CommandInfo(OnCommand)
        {
            HelpMessage = "Öffnet das PeepingTim-Fenster."
        });

        CommandManager.AddHandler(CommandName2, new CommandInfo(OnCommand)
        {
            HelpMessage = "Öffnet das PeepingTim-Fenster."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // Register update method
        Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName1);
        CommandManager.RemoveHandler(CommandName2);

        Framework.Update -= OnUpdate;
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
    }

    private void DrawUI() => WindowSystem.Draw();

    private void OnUpdate(IFramework framework)
    {
        if (ClientState.LocalPlayer == null)
            return;

        ulong localPlayerId = ClientState.LocalPlayer.GameObjectId;

        // Temporäre Liste der aktuellen Betrachter
        var currentlyLookingAtMe = new HashSet<string>();

        foreach (var obj in ObjectTable)
        {
            if (obj is IPlayerCharacter character && character.GameObjectId != localPlayerId)
            {
                if (character.TargetObjectId == localPlayerId)
                {
                    string key = GetPlayerKey(character);

                    currentlyLookingAtMe.Add(key);

                    if (!viewers.ContainsKey(key))
                    {
                        // Neuer Betrachter
                        viewers[key] = new ViewerInfo
                        {
                            Name = character.Name.TextValue,
                            World = character.HomeWorld.GameData?.Name ?? "Unknown",
                            IsActive = true,
                            FirstSeen = DateTime.Now,
                            LastSeen = DateTime.Now
                        };

                        // Optionally play a sound when a new viewer starts watching
                        if (SoundEnabled)
                        {
                            // Play sound notification here if desired
                        }
                    }
                    else
                    {
                        // Bereits bekannter Betrachter, Status aktualisieren
                        viewers[key].IsActive = true;
                        viewers[key].LastSeen = DateTime.Now;
                    }
                }
            }
        }

        // Betrachter, die nicht mehr schauen, als inaktiv markieren
        foreach (var key in viewers.Keys.ToList())
        {
            if (!currentlyLookingAtMe.Contains(key))
            {
                viewers[key].IsActive = false;
            }
        }

        // Entfernen von Betrachtern, die länger als eine bestimmte Zeit nicht mehr aktiv sind (optional)
        // Beispiel: Betrachter entfernen, die seit mehr als 10 Minuten nicht mehr gesehen wurden
        // viewers = viewers.Where(v => (DateTime.Now - v.Value.LastSeen).TotalMinutes <= 10).ToDictionary(v => v.Key, v => v.Value);
    }

    // Methoden zum Abrufen der Betrachter
    public List<ViewerInfo> GetViewers()
    {
        // Sortieren der Betrachter: Zuerst aktive, dann nach Zeitpunkt
        return viewers.Values
            .OrderByDescending(v => v.IsActive)
            .ThenByDescending(v => v.FirstSeen)
            .Take(15) // Begrenzen auf die Top 15
            .ToList();
    }

    private string GetPlayerKey(IPlayerCharacter character)
    {
        // Eindeutiger Schlüssel aus Name und Welt
        string worldName = character.HomeWorld.GameData?.Name ?? "Unknown";
        return $"{character.Name.TextValue}@{worldName}";
    }

    // Methode zum Auswählen eines Ziels
    public void TargetCharacter(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            var character = FindCharacterInObjectTable(viewer);
            if (character != null)
            {
                TargetManager.Target = character;
            }
            else
            {
                ChatGui.PrintError($"Charakter {viewer.Name} nicht gefunden.");
            }
        }
    }

    // Methode zum Hervorheben eines Charakters
    public void HighlightCharacter(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            var character = FindCharacterInObjectTable(viewer);
            if (character != null)
            {
                TargetManager.MouseOverTarget = character;
            }
        }
    }

    // Hilfsmethode zum Finden eines Charakters in der ObjectTable
    private IPlayerCharacter? FindCharacterInObjectTable(ViewerInfo viewer)
    {
        return ObjectTable.FirstOrDefault(obj =>
            obj is IPlayerCharacter pc &&
            pc.Name.TextValue == viewer.Name &&
            pc.HomeWorld.GameData?.Name == viewer.World) as IPlayerCharacter;
    }

    // Methode zum Senden von Chat-Befehlen
    public void SendChatCommand(string command, ViewerInfo viewer)
    {
        if (viewer != null)
        {
            string name = $"{viewer.Name}@{viewer.World}";
            string fullCommand = $"/{command} {name}";
            CommandManager.ProcessCommand(fullCommand);
        }
    }

    public void OpenChatWith(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            string message = $"/tell {viewer.Name}@{viewer.World} ";
            //ChatGui.OpenChat(message, false);
        }
    }

    // Methode zum Kopieren des Spielernamens in die Zwischenablage
    public void CopyPlayerName(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            string name = $"{viewer.Name}@{viewer.World}";
            ImGui.SetClipboardText(name);
            ChatGui.Print($"Kopiert {name} in die Zwischenablage.");
        }
    }

    // ViewerInfo-Klasse zur Speicherung von Betrachterinformationen
    public class ViewerInfo
    {
        public string Name { get; set; }
        public string World { get; set; }
        public bool IsActive { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}

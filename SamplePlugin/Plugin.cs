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
using Lumina.Excel.Sheets;
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;

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
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;

    

    private const string CommandName1 = "/ptim";
    private const string CommandName2 = "/peepingtim";
    private Dictionary<string, MessageWindow> messageWindows = new();

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("PeepingTim");
    private MainWindow MainWindow { get; init; }

    // Dictionary to store viewer information
    private Dictionary<string, ViewerInfo> viewers = new();

    // Cache for world names
    private readonly Dictionary<uint, string> worldNames = new();

    public bool SoundEnabled = false;

    // For update rate control
    private long lastUpdateTick = 0;
    private const int UpdateIntervalMs = 100;

    private long lastLoadingTick = 0;
    private const int LoadingIntervalMs = 1500;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(MainWindow);

        // Add commands
        CommandManager.AddHandler(CommandName1, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens P-Tim Window."
        });

        CommandManager.AddHandler(CommandName2, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens P-Tim Window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // Load world names
        var worldSheet = DataManager.GetExcelSheet<World>();
        if (worldSheet != null)
        {
            foreach (var world in worldSheet)
            {
                worldNames[world.RowId] = world.Name.ExtractText();
            }
        }

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
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
    }

    private void DrawUI() => WindowSystem.Draw();

    public void PrintCommands()
    {
        foreach (var command in CommandManager.Commands)
        {
            ChatGui.Print($"{command.Key}: {command.Value.HelpMessage}");
        }
    }

    private void OnUpdate(IFramework framework)
    {
        long currentTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (currentTick - lastUpdateTick < UpdateIntervalMs)
        {
            return; // Noch nicht genug Zeit vergangen
        }
        lastUpdateTick = currentTick;

        if (currentTick - lastLoadingTick >= LoadingIntervalMs)
        {
            lastLoadingTick = currentTick;
            var viewers = GetViewers();
            var loadedPcs = FindLoadedViewersInObjectTable(viewers);
            foreach (var viewer in viewers)
            {
               viewer.isLoaded = loadedPcs.Contains(viewer);
            }
        }

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

                    if (!viewers.TryGetValue(key, out var viewerInfo))
                    {
                        // Neuer Betrachter
                        viewerInfo = new ViewerInfo
                        {
                            Name = character.Name.TextValue,
                            World = GetWorldName(character.HomeWorld.RowId),
                            IsActive = true,
                            isLoaded = true,
                            FirstSeen = DateTime.Now,
                            LastSeen = DateTime.Now
                        };
                        viewers[key] = viewerInfo;

                        // Optionally play a sound when a new viewer starts watching
                        if (SoundEnabled)
                        {
                            // Play sound notification here if desired
                        }
                    }
                    else
                    {
                        // Bereits bekannter Betrachter, Status aktualisieren
                        viewerInfo.IsActive = true;
                        viewerInfo.LastSeen = DateTime.Now;
                    }
                }
            }
        }

        // Betrachter, die nicht mehr schauen, als inaktiv markieren
        foreach (var viewerInfo in viewers.Values)
        {
            if (!currentlyLookingAtMe.Contains(GetViewerKey(viewerInfo)))
            {
                viewerInfo.IsActive = false;
            }
        }

        // Optional: Entfernen von Betrachtern, die seit mehr als 10 Minuten nicht mehr gesehen wurden
        // viewers = viewers.Where(v => (DateTime.Now - v.Value.LastSeen).TotalMinutes <= 10).ToDictionary(v => v.Key, v => v.Value);
    }

    // Methoden zum Abrufen der Betrachter
    public List<ViewerInfo> GetViewers()
    {
        // Sortieren der Betrachter: Zuerst aktive, dann nach Zeitpunkt
        return viewers.Values
            .OrderByDescending(v => v.IsActive)
            .ThenByDescending(v => v.LastSeen)
            .Take(15) // Begrenzen auf die Top 15
            .ToList();
    }

    public string GetWorldName(uint rowId)
    {
        if (worldNames.TryGetValue(rowId, out var worldName))
        {
            return worldName;
        }
        return "Unknown";
    }

    private string GetPlayerKey(IPlayerCharacter character)
    {
        // Eindeutiger Schlüssel aus Name und Welt
        string worldName = GetWorldName(character.HomeWorld.RowId);
        return $"{character.Name.TextValue}@{worldName}";
    }

    private string GetViewerKey(ViewerInfo viewer)
    {
        return $"{viewer.Name}@{viewer.World}";
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
        foreach (var obj in ObjectTable)
        {
            if (obj is IPlayerCharacter pc)
            {
                if (pc.Name.TextValue == viewer.Name && GetWorldName(pc.HomeWorld.RowId) == viewer.World)
                {
                    return pc;
                }
            }
        }
        return null;
    }

    private List<ViewerInfo> FindLoadedViewersInObjectTable(List<ViewerInfo> viewerList)
    {
        var loadedViewers = new List<ViewerInfo>();

        // Erstellen eines HashSets von Spieler-Schlüsseln (Name@Welt) aus der ObjectTable
        var objectTableKeys = new HashSet<string>();
        foreach (var obj in ObjectTable)
        {
            if (obj is IPlayerCharacter pc)
            {
                string worldName = GetWorldName(pc.HomeWorld.RowId);
                string key = $"{pc.Name.TextValue}@{worldName}";
                objectTableKeys.Add(key);
            }
        }

        // Überprüfen, welche Viewer noch geladen sind
        foreach (var viewer in viewerList)
        {
            string key = $"{viewer.Name}@{viewer.World}";
            if (objectTableKeys.Contains(key))
            {
                loadedViewers.Add(viewer);
            }
        }

        return loadedViewers;
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

    public void OpenChatWith(ViewerInfo viewer, string message)
    {
        if (viewer != null)
        {
            string command = $"/tell {viewer.Name}@{viewer.World} {message}";
            if (!CommandManager.ProcessCommand(command))
            {
                ChatGui.PrintError($"error with executing: {command}");
            }
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

    // Methode zum Öffnen eines MessageWindow
    public void OpenMessageWindow(ViewerInfo viewer)
    {
        string key = $"{viewer.Name}@{viewer.World}";

        if (!messageWindows.ContainsKey(key))
        {
            var messageWindow = new MessageWindow(this, viewer);
            WindowSystem.AddWindow(messageWindow);
            messageWindows[key] = messageWindow;
        }
        else
        {
            // Falls das Fenster bereits geöffnet ist, bringen wir es in den Fokus
            messageWindows[key].IsOpen = true;
        }
    }

    // Methode zum Schließen eines MessageWindow
    public void CloseMessageWindow(ViewerInfo viewer)
    {
        string key = $"{viewer.Name}@{viewer.World}";
        if (messageWindows.ContainsKey(key))
        {
            var window = messageWindows[key];
            WindowSystem.RemoveWindow(window);
            window.Dispose();
            messageWindows.Remove(key);
        }
    }

    // ViewerInfo-Klasse zur Speicherung von Betrachterinformationen
    public class ViewerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string World { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool isLoaded { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
    }
}

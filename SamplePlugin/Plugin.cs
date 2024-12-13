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
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ECommons;
using ECommons.Automation;
using ECommons.GameFunctions;
using NAudio.Wave;
using System.Threading;
using System.Linq;
using ECommons.Logging;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.Sound;
using ECommons.EzEventManager;

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
    private const string CommandName3 = "/ptimconfig";

    private Dictionary<string, MessageWindow> messageWindows = new();

    private Dictionary<string, List<ViewerInfo>> stalkerViewers = new();
    private Dictionary<string, StalkerWindow> stalkerWindows = new();

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Peeping Tim");
    private MainWindow MainWindow { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private Helpers.SoundManager SoundManager { get; init; }

    private Dictionary<string, ViewerInfo> viewers = new();

    private readonly Dictionary<uint, string> worldNames = new();

    private long lastUpdateTick = 0;
    private const int UpdateIntervalMs = 100;

    private long lastLoadingTick = 0;
    private const int LoadingIntervalMs = 1500;

    private bool firstDrawn = false;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        SoundManager = new Helpers.SoundManager(this);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName1, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens P-Tim Window."
        });

        CommandManager.AddHandler(CommandName2, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens P-Tim Window."
        });

        CommandManager.AddHandler(CommandName3, new CommandInfo(OnConfig)
        {
            HelpMessage = "Opens P-Tim Config."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfig;
        PluginInterface.UiBuilder.OpenMainUi += DrawMain;

        if (Configuration.StartOnStartup)
        {
            ClientState.Login += OnLogin;
            ClientState.TerritoryChanged += OnTerritoryChanged;
        }

        var worldSheet = DataManager.GetExcelSheet<World>();
        if (worldSheet != null)
        {
            foreach (var world in worldSheet)
            {
                worldNames[world.RowId] = world.Name.ExtractText();
            }
        }

        ECommonsMain.Init(PluginInterface, this);
        SoundManager.CheckSoundFile();

        Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName1);
        CommandManager.RemoveHandler(CommandName2);
        CommandManager.RemoveHandler(CommandName3);

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= DrawConfig;
        PluginInterface.UiBuilder.OpenMainUi -= DrawMain;

        Framework.Update -= OnUpdate;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
    }

    private void OnConfig(string command, string args)
    {
        ConfigWindow.IsOpen = true;
    }

    private void OnLogin()
    {
        TryOpenMainWindow();
    }

    private void OnTerritoryChanged(ushort territoryId)
    {
        TryOpenMainWindow();
    }

    private void TryOpenMainWindow()
    {
        if (ClientState.IsLoggedIn && ClientState.LocalPlayer != null && !firstDrawn)
        {
            ClientState.Login -= OnLogin;
            ClientState.TerritoryChanged -= OnTerritoryChanged;
            firstDrawn = true;
            MainWindow.IsOpen = true;
        }
    }


    private void DrawUI() => WindowSystem.Draw();
    private void DrawConfig() => OnConfig("/ptimconfig", "");
    private void DrawMain() => OnCommand("/ptim", "");

    public void PrintCommands()
    {
        foreach (var command in CommandManager.Commands)
        {
            ChatGui.Print($"{command.Key}: {command.Value.HelpMessage}");
        }
    }

    public void SendError(string message)
    {
        ChatGui.PrintError(message);
    }

    public void OpenStalkWindow(ViewerInfo viewer)
    {
        string name = $"{viewer.Name}@{viewer.World}";
        if (stalkerViewers.ContainsKey(name)) return;

        var stalkerWindow = new StalkerWindow(this, viewer);
        WindowSystem.AddWindow(stalkerWindow);
        stalkerWindows[name] = stalkerWindow;
    }

    private void OnUpdate(IFramework framework)
    {
        long currentTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (currentTick - lastUpdateTick < UpdateIntervalMs)
        {
            return;
        }
        lastUpdateTick = currentTick;

        if (currentTick - lastLoadingTick >= LoadingIntervalMs)
        {
            lastLoadingTick = currentTick;
            var viewers = GetViewers();
            var loadedPcs = FindLoadedViewersInObjectTable(viewers);
            foreach (var viewer in viewers)
            {
                if (!loadedPcs.Keys.Contains(viewer))
                {
                    viewer.isLoaded = false;
                } else
                {
                    viewer.isLoaded = true;
                    viewer.lastKnownGameObjectId = loadedPcs[viewer];
                }
               
            }
        }

        if (ClientState.LocalPlayer == null)
            return;

        ulong localPlayerId = ClientState.LocalPlayer.GameObjectId;
        List<ulong> stalkedIds = new List<ulong>();
        
        foreach( var v in stalkerViewers )
        {
            if (v.Key)
        }

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
                        viewerInfo = new ViewerInfo
                        {
                            Name = character.Name.TextValue,
                            World = GetWorldName(character.HomeWorld.RowId),
                            IsActive = true,
                            isLoaded = true,
                            isFocused = false,
                            soundPlayed = false,
                            FirstSeen = DateTime.Now,
                            LastSeen = DateTime.Now,
                            lastKnownGameObjectId = character.GameObjectId,
                        };
                        viewers[key] = viewerInfo;

                    }
                    else
                    {
                        viewerInfo.IsActive = true;
                        viewerInfo.LastSeen = DateTime.Now;
                    }
                    if (Configuration.SoundEnabled && !viewerInfo.soundPlayed)
                    {
                        try
                        {
                            SoundManager.PlaySound();
                            viewerInfo.soundPlayed = true;
                        }
                        catch (Exception ex)
                        {
                            ChatGui.PrintError($"Error Sound 3: {ex.Message}");
                        }
                    }

                }
            }
        }

        foreach (var viewerInfo in viewers.Values)
        {
            if (!currentlyLookingAtMe.Contains(GetViewerKey(viewerInfo)))
            {
                viewerInfo.IsActive = false;
                viewerInfo.soundPlayed = false;
            }
        }
    }

    public List<ViewerInfo> GetViewers()
    {
        return viewers.Values
            .OrderByDescending(v => v.IsActive)
            .ThenByDescending(v => v.LastSeen)
            .Take(15)
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
        string worldName = GetWorldName(character.HomeWorld.RowId);
        return $"{character.Name.TextValue}@{worldName}";
    }

    private string GetViewerKey(ViewerInfo viewer)
    {
        return $"{viewer.Name}@{viewer.World}";
    }

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
                ChatGui.PrintError($"Character {viewer.Name} can't be targeted.");
            }
        }
    }

    public void HighlightCharacter(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            var character = FindCharacterInObjectTable(viewer);
            if (character != null)
            {
                if (viewer.isFocused)
                {
                    TargetManager.FocusTarget = null;
                } else
                {
                    TargetManager.FocusTarget = character;
                }
                viewer.isFocused = !viewer.isFocused;
            }
        }
    }

    public IPlayerCharacter? FindCharacterInObjectTable(ViewerInfo viewer)
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

    private Dictionary<ViewerInfo, ulong> FindLoadedViewersInObjectTable(List<ViewerInfo> viewerList)
    {
        var loadedViewers = new Dictionary<ViewerInfo, ulong>();

        var objectTableKeys = new Dictionary<string, ulong>();
        foreach (var obj in ObjectTable)
        {
            if (obj is IPlayerCharacter pc)
            {
                string worldName = GetWorldName(pc.HomeWorld.RowId);
                string key = $"{pc.Name.TextValue}@{worldName}";
                objectTableKeys.Add(key, pc.GameObjectId);
            }
        }

        foreach (var viewer in viewerList)
        {
            string key = $"{viewer.Name}@{viewer.World}";
            if (objectTableKeys.Keys.Contains(key))
            {
                loadedViewers.Add(viewer, objectTableKeys[key]);
            }
        }

        return loadedViewers;
    }


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
            Chat.Instance.SendMessage(command);
        }
    }

    public void CopyPlayerName(ViewerInfo viewer)
    {
        if (viewer != null)
        {
            string name = $"{viewer.Name}@{viewer.World}";
            ImGui.SetClipboardText(name);
            ChatGui.Print($"Kopiert {name} in die Zwischenablage.");
        }
    }

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
            messageWindows[key].IsOpen = true;
        }
    }

    public bool IsMainWindowOpen()
    {
        return MainWindow.IsOpen;
    }

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

    public class ViewerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string World { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool isLoaded { get; set; }
        public bool isFocused { get; set; }
        public bool soundPlayed { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public ulong lastKnownGameObjectId { get; set; }
    }
}

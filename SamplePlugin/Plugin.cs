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
using Dalamud.Bindings.ImGui;
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
using ECommons.Logging;
using System.IO;
using FFXIVClientStructs.FFXIV.Client.Sound;
using ECommons.EzEventManager;
using FFXIVClientStructs.FFXIV.Common.Lua;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ECommons.DalamudServices;
using System.Threading.Tasks;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Utility;
using ECommons.ChatMethods;

namespace PeepingTim
{
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
        [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;

        private const string CommandName1 = "/ptim";
        private const string CommandName2 = "/peepingtim";
        private const string CommandName3 = "/ptimconfig";

        // Statt ViewerInfo als Key => String als Key ("Name@World")
        // "Normale" Viewer
        private Dictionary<string, ViewerInfo> viewers = new();

        private Dictionary<string, ViewerInfo> unknownViewer = new();

        // Stalker: Key = "Name@World" des Stalkers
        // Value = Dictionary mit Key="ViewerName@World" und Value=ViewerInfo
        private Dictionary<string, Dictionary<string, ViewerInfo>> stalkerViewers = new();

        // Stalker-Fenster pro Stalker
        private Dictionary<string, StalkerWindow> stalkerWindows = new();

        // speichert, auf wen der Stalker gerade schaut: Key = "StalkerName@World", Value = "TargetName@World" oder null
        private Dictionary<string, ViewerInfo?> stalkerLooksAt = new();


        // Fenster für Chat etc.
        private Dictionary<string, MessageWindow> messageWindows = new();

        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("Peeping Tim");
        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        private Helpers.SoundManager SoundManager { get; init; }

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

            ContextMenu.OnMenuOpened += OnMenu;

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

        private void OnMenu(IMenuOpenedArgs args)
        {
            if (args.MenuType == ContextMenuType.Inventory || !Configuration.NoNoNo) 
                return;

            try
            {
                MenuTargetDefault target = (MenuTargetDefault)args.Target;
                if (target.TargetObject != null && target.TargetObject is IPlayerCharacter pc)
                {
                    args.AddMenuItem(new()
                    {
                        Name = "Stalk (PTim)",
                        OnClicked = TestMenuItem,
                        Prefix = SeIconChar.BoxedLetterP,
                        Priority = 50
                    });
                }
            }
            catch (Exception ex)
            {
                ChatGui.PrintError(ex.Message);
            }
        }

        private void TestMenuItem(IMenuItemClickedArgs args)
        {
            try
            {
                MenuTargetDefault target = (MenuTargetDefault)args.Target;
                if (target.TargetObject != null && target.TargetObject is IPlayerCharacter pc) 
                {
                    OpenStalkWindow(CreateViewer(pc));
                }
            }
            catch (Exception ex)
            {
                ChatGui.PrintError(ex.Message);
            }
            //OpenStalkWindow(CreateViewer(args.));
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
        public void DrawConfig() => OnConfig("/ptimconfig", "");
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

        #region Stalker-Management

        public void CloseStalkerWindow(ViewerInfo viewer)
        {
            string stalkerKey = GetViewerKey(viewer);
            bool shouldContinue = false;

            // Schon vorhanden?
            if(unknownViewer.ContainsKey(stalkerKey))
            {
                unknownViewer.Remove(stalkerKey);
                shouldContinue = true;
            } 
            if (stalkerViewers.ContainsKey(stalkerKey))
            {
                stalkerViewers.Remove(stalkerKey);
                shouldContinue = true;
            }

            if (!shouldContinue)
                return;

            WindowSystem.RemoveWindow(stalkerWindows[stalkerKey]);

            if (!stalkerWindows.ContainsKey(stalkerKey))
                return;

            stalkerWindows.Remove(stalkerKey);
        }

        public void OpenStalkWindow(ViewerInfo viewer)
        {
            // Der Key des Stalkers
            string stalkerKey = GetViewerKey(viewer);

            // Schon vorhanden?
            if (stalkerViewers.ContainsKey(stalkerKey))
                return;

            if (!viewers.ContainsKey(stalkerKey))
            {
                unknownViewer.Add(stalkerKey, viewer);
            }

            // Neues Dictionary für alle Viewer, die diesen Stalker anschauen
            stalkerViewers[stalkerKey] = new Dictionary<string, ViewerInfo>();
            stalkerLooksAt[stalkerKey] = null;

            // Eigenes Window
            var stalkerWindow = new StalkerWindow(this, viewer);
            WindowSystem.AddWindow(stalkerWindow);
            stalkerWindows[stalkerKey] = stalkerWindow;
        }

        public void i(ViewerInfo v)
        {
            string stalkerKey = GetViewerKey(v);

            if (stalkerWindows.TryGetValue(stalkerKey, out var win))
            {
                win.IsOpen = false;
                WindowSystem.RemoveWindow(win);
                stalkerWindows.Remove(stalkerKey);
            }

            if (stalkerLooksAt.ContainsKey(stalkerKey))
                stalkerLooksAt.Remove(stalkerKey);

            if (stalkerViewers.ContainsKey(stalkerKey))
                stalkerViewers.Remove(stalkerKey);
        }

        public List<ViewerInfo> GetStalkerViewers(ViewerInfo user)
        {
            var result = new List<ViewerInfo>();
            string stalkerKey = GetViewerKey(user);

            if (!stalkerViewers.ContainsKey(stalkerKey))
                return result;

            foreach (var v in stalkerViewers[stalkerKey].Values)
            {
                result.Add(v);
            }

            return result
                .OrderByDescending(v => v.IsActive)
                .ThenByDescending(v => v.LastSeen)
                .Take(15)
                .ToList();
        }

        public ViewerInfo? GetStalkerTarget(ViewerInfo stalker)
        {
            // Gibt zurück, wen der Stalker gerade anschaut (sofern vorhanden)
            string stalkerKey = GetViewerKey(stalker);
            if (stalkerLooksAt.TryGetValue(stalkerKey, out var targetKey))
            {
                return targetKey;
            }
            return null;
        }

        #endregion

        #region Update-Loop

        private void OnUpdate(IFramework framework)
        {
            long currentTick = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (currentTick - lastUpdateTick < UpdateIntervalMs)
                return;
            lastUpdateTick = currentTick;

            // AllViewer-Loading Check (alle 1,5 Sek)
            if (currentTick - lastLoadingTick >= LoadingIntervalMs)
            {
                lastLoadingTick = currentTick;
                var allKnownViewers = GetAllViewers();
                var loadedPcs = FindLoadedViewersInObjectTable(allKnownViewers);
                foreach (var viewer in allKnownViewers)
                {
                    string key = GetViewerKey(viewer);
                    if (!loadedPcs.ContainsKey(key))
                    {
                        viewer.isLoaded = false;
                    }
                    else
                    {
                        viewer.isLoaded = true;
                        viewer.lastKnownGameObjectId = loadedPcs[key];
                    }
                }
            }

            if (ClientState.LocalPlayer == null) return;
            ulong localPlayerId = ClientState.LocalPlayer.GameObjectId;

            // Stalker, die wir aktuell tracken
            // Key: GameObjectId, Value: "StalkerKey (Name@World)"
            var stalkedIds = new Dictionary<ulong, string>();

            // Schauen, ob jeder Stalker noch geladen ist
            var toClose = new List<string>();
            foreach (var kv in stalkerViewers)
            {
                if (viewers.TryGetValue(kv.Key, out var stInfo) && stInfo.isLoaded)
                {
                    stalkedIds[stInfo.lastKnownGameObjectId] = kv.Key;
                } else if (unknownViewer.TryGetValue(kv.Key, out var stInfoTwo) && stInfoTwo.isLoaded)
                {
                    stalkedIds[stInfoTwo.lastKnownGameObjectId] = kv.Key;
                } else
                {
                    toClose.Add(kv.Key);
                }
            }

            //// Falls Stalker entladen, Fenster schließen
            //foreach (var sc in toClose)
            //{
            //    var fallback = new ViewerInfo { Name = sc, World = "(???)" };
            //    //CloseStalkerWindow(fallback);
            //}

            // Wer guckt mich an? 
            var currentlyLookingAtMe = new HashSet<string>();

            // Wer guckt den Stalker an?
            var currentlyLookingAtStalker = new Dictionary<string, HashSet<string>>();
            // vorinitialisieren
            foreach (var stKey in stalkerViewers.Keys)
            {
                currentlyLookingAtStalker[stKey] = new HashSet<string>();
            }

            // Durch alle Objekte in der ObjectTable
            foreach (var obj in ObjectTable)
            {
                if (obj is IPlayerCharacter character)
                {
                    // 1) Schauen, ob mich dieser Character (LocalPlayer) anvisiert
                    if (character.TargetObjectId == localPlayerId && character.GameObjectId != localPlayerId)
                    {
                        string charKey = GetPlayerKey(character);
                        currentlyLookingAtMe.Add(charKey);

                        if (!viewers.TryGetValue(charKey, out var vInfo))
                        {
                            viewers[charKey] = CreateViewer(character);
                        }
                        else
                        {
                            vInfo.IsActive = true;
                            vInfo.LastSeen = DateTime.Now;
                        }
                        try
                        {
                            if (vInfo == null)
                                continue;
                            if (Configuration.SoundEnabled && !vInfo.soundPlayed)
                            {
                                try
                                {
                                    SoundManager.PlaySound();
                                    vInfo.soundPlayed = true;
                                }
                                catch (Exception ex)
                                {
                                    ChatGui.PrintError($"Error Sound 3: {ex.Message}");
                                }
                            }
                        } 
                        catch (Exception ex)
                        {
                            ChatGui.PrintError($"Error Sound 1: {ex.Message}");
                        }
                        // Sound abspielen
                    }

                    // 2) Schauen, ob dieser Char jemanden anvisiert, der ein Stalker ist
                    if (stalkedIds.TryGetValue(character.TargetObjectId, out var stalkerKey))
                    {
                        // d.h. character guckt einen Stalker an
                        string charKey = GetPlayerKey(character);
                        currentlyLookingAtStalker[stalkerKey].Add(charKey);

                        if (!stalkerViewers[stalkerKey].TryGetValue(charKey, out var sViewerInfo))
                        {
                            stalkerViewers[stalkerKey][charKey] = CreateViewer(character);
                        }
                        else
                        {
                            sViewerInfo.IsActive = true;
                            sViewerInfo.LastSeen = DateTime.Now;
                        }
                    }

                    // 3) Falls der Character selbst ein Stalker ist, schauen wir, wen er anvisiert
                    if (stalkedIds.TryGetValue(character.GameObjectId, out var stKey2))
                    {
                        // stKey2 = "StalkerName@World"
                        if (character.TargetObject == null)
                        {
                            stalkerLooksAt[stKey2] = null;
                        }
                        else
                        {
                            // Er guckt irgendwen an
                            // wir müssen per ObjectTable dessen Key herausfinden
                            var possibleTarget = ObjectTable.SearchById(character.TargetObjectId) as IPlayerCharacter;
                            if (possibleTarget == null)
                            {
                                // Falls gar kein PlayerCharacter => leer
                                stalkerLooksAt[stKey2] = null;
                            }
                            else
                            {                            
                                if (stalkerLooksAt[stKey2] != null)
                                {
                                    if (GetViewerKey(stalkerLooksAt[stKey2]!) != $"{possibleTarget.Name.TextValue}@{GetWorldName(possibleTarget.HomeWorld.RowId)}")
                                    {
                                        stalkerLooksAt[stKey2] = CreateViewer(possibleTarget);
                                    }
                                } else
                                {
                                    stalkerLooksAt[stKey2] = CreateViewer(possibleTarget);
                                }

                            }
                        }
                    }
                }
            }

            // 4) Wer mich NICHT mehr anvisiert => isActive = false
            foreach (var vInfo in viewers.Values)
            {
                string vKey = GetViewerKey(vInfo);
                if (!currentlyLookingAtMe.Contains(vKey))
                {
                    vInfo.IsActive = false;
                    vInfo.soundPlayed = false;
                }
            }

            // 5) Wer den Stalker NICHT mehr anvisiert => isActive = false
            foreach (var kv in stalkerViewers)
            {
                // kv.Key = "StalkerKey"
                foreach (var sV in kv.Value)
                {
                    // sV.Key = "ViewerKey", sV.Value = ViewerInfo
                    if (!currentlyLookingAtStalker[kv.Key].Contains(sV.Key))
                    {
                        sV.Value.IsActive = false;
                    }
                }
            }
        }

        #endregion

        #region Getter-Funktionen

        // Alle Viewer (normale + die in den Stalker-Dictionaries) + ggf. "stalkerLooksAt"
        public HashSet<ViewerInfo> GetAllViewers()
        {
            var set = new HashSet<ViewerInfo>();

            // "normale" Viewer
            foreach (var v in viewers.Values)
                set.Add(v);

            // Alle Viewer aus den Stalker-View Dictionaries
            foreach (var stDict in stalkerViewers.Values)
            {
                foreach (var v in stDict.Values)
                    set.Add(v);
            }

            foreach (var v in unknownViewer.Values)
            {
                set.Add(v);
            }

            // Jeder Stalker guckt ggf. jmd. an
            foreach (var targKey in stalkerLooksAt.Values)
            {
                if (targKey != null)
                {
                    set.Add(targKey);
                }
            }

            return set;
        }

        // Gibt nur die 15 „aktuellsten“ „normalen“ Viewer zurück
        public List<ViewerInfo> GetViewers()
        {
            return viewers.Values
                .OrderByDescending(v => v.IsActive)
                .ThenByDescending(v => v.LastSeen)
                .Take(15)
                .ToList();
        }

        #endregion

        #region Char-Funktionen

        public void TargetCharacter(ViewerInfo viewer)
        {
            if (viewer == null) return;

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

        public void DoteViewer(ViewerInfo viewer)
        {
            if (viewer == null) return;

            IPlayerCharacter? x = ObjectTable.SearchById(viewer.lastKnownGameObjectId) as IPlayerCharacter;

            if (x == null) return;

            if (viewer.isLoaded && x.IsTargetable)
            {
                TargetManager.Target = x;
                Task.Delay(1000);
                Svc.Framework.RunOnTick(() =>
                {
                    Chat.SendMessage("/dote");
                });
            }
        }

        public void HighlightCharacter(ViewerInfo viewer)
        {
            if (viewer == null) return;

            var character = FindCharacterInObjectTable(viewer);
            if (character != null)
            {
                if (viewer.isFocused)
                {
                    TargetManager.FocusTarget = null;
                }
                else
                {
                    TargetManager.FocusTarget = character;
                }
                viewer.isFocused = !viewer.isFocused;
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

        private Dictionary<string, ulong> FindLoadedViewersInObjectTable(HashSet<ViewerInfo> viewerList)
        {
            var loadedViewers = new Dictionary<string, ulong>();
            var objectTableKeys = new Dictionary<string, ulong>();

            // Alle Spieler im Objekt-Tisch "einscannen"
            foreach (var obj in ObjectTable)
            {
                if (obj is IPlayerCharacter pc)
                {
                    string key = GetPlayerKey(pc);
                    objectTableKeys[key] = pc.GameObjectId;
                }
            }

            // Für alle bekannten Viewer checken, ob sie in objectTableKeys stecken
            foreach (var viewer in viewerList)
            {
                string vKey = GetViewerKey(viewer);
                if (objectTableKeys.ContainsKey(vKey))
                {
                    loadedViewers[vKey] = objectTableKeys[vKey];
                }
            }

            return loadedViewers;
        }

        #endregion

        #region Chat etc.

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
                Svc.Framework.RunOnTick(() =>
                {
                    Chat.SendMessage(command);
                });
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
            string key = GetViewerKey(viewer);

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

        public void CloseMessageWindow(ViewerInfo viewer)
        {
            string key = GetViewerKey(viewer);
            if (messageWindows.ContainsKey(key))
            {
                var window = messageWindows[key];
                WindowSystem.RemoveWindow(window);
                window.Dispose();
                messageWindows.Remove(key);
            }
        }

        public bool IsMainWindowOpen()
        {
            return MainWindow.IsOpen;
        }

        #endregion

        #region Helper

        public string GetWorldName(uint rowId)
        {
            if (worldNames.TryGetValue(rowId, out var worldName))
            {
                return worldName;
            }
            return "Unknown";
        }

        // Key für Dictionary = "Name@World"
        public string GetViewerKey(ViewerInfo viewer)
        {
            return $"{viewer.Name}@{viewer.World}";
        }

        // Für IPlayerCharacter
        public string GetPlayerKey(IPlayerCharacter character)
        {
            string w = GetWorldName(character.HomeWorld.RowId);
            return $"{character.Name.TextValue}@{w}";
        }

        #endregion

        #region Data-Class

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
            public ulong cid { get; set; } = 0;
        }

        public ViewerInfo CreateViewer(IPlayerCharacter x)
        {
            ulong cid = 0;
            unsafe
            {
                cid = x.Struct()->ContentId;
            }

            return new ViewerInfo
            {
                Name = x.Name.TextValue,
                World = GetWorldName(x.HomeWorld.RowId),
                IsActive = true,
                isLoaded = true,
                isFocused = false,
                soundPlayed = false,
                FirstSeen = DateTime.Now,
                LastSeen = DateTime.Now,
                lastKnownGameObjectId = x.GameObjectId,
                cid = cid
            };
        }

        public ViewerInfo CreateTestViewer(string Name, string WordlName)
        {
            return new ViewerInfo
            {
                Name = Name,
                World = WordlName,
                IsActive = true,
                isLoaded = true,
                isFocused = false,
                soundPlayed = false,
                FirstSeen = DateTime.Now,
                LastSeen = DateTime.Now,
                lastKnownGameObjectId = 123,
            };
        }

        #endregion
    }
}

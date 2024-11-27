using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Runtime.InteropServices;
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
using FFXIVClientStructs.FFXIV.Client.Game.Object;

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

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("PeepingTim");
    private MainWindow MainWindow { get; init; }

    // Lists for current and past viewers
    private List<ulong> currentViewers = new();
    private List<ulong> pastViewers = new();

    public bool SoundEnabled = false;

    // Dictionary to store timestamps of when a viewer started watching
    private Dictionary<ulong, DateTime> viewerTimestamps = new();

    private bool test = true;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(MainWindow);

        // Add commands
        CommandManager.AddHandler(CommandName1, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the PeepingTim window."
        });

        CommandManager.AddHandler(CommandName2, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the PeepingTim window."
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
        var newCurrentViewers = new List<ulong>();

        foreach (var obj in ObjectTable)
        {
            if (obj is IGameObject gameObject)
            {
                if (gameObject is ICharacter character && character.GameObjectId != localPlayerId)
                {
                    if (character.TargetObjectId == localPlayerId)
                    {
                        ulong charId = character.GameObjectId;
                        newCurrentViewers.Add(charId);

                        if (!pastViewers.Contains(charId))
                        {
                            pastViewers.Add(charId);
                        }
                    }
                }
            }
        }

        // Update viewer timestamps
        foreach (var viewerId in newCurrentViewers)
        {
            viewerTimestamps[viewerId] = DateTime.Now;
            if (test)
            {
                viewerTimestamps[localPlayerId] = DateTime.Now;
                test = false;
            }

            // Optionally play a sound when a new viewer starts watching
            if (SoundEnabled)
            {
                // Implement sound notification here if desired
            }
        }

        // Remove timestamps for viewers who are no longer watching
        var allKnownViewers = newCurrentViewers.Concat(pastViewers).Distinct().ToList();
        var keysToRemove = viewerTimestamps.Keys.Except(allKnownViewers).ToList();
        foreach (var key in keysToRemove)
        {
            viewerTimestamps.Remove(key);
        }

        currentViewers = newCurrentViewers;
    }

    // Methods to retrieve viewers
    public List<ICharacter> GetCurrentViewers()
    {
        var viewers = new List<ICharacter>();
        foreach (var objId in currentViewers)
        {
            var gameObject = ObjectTable.SearchById(objId);
            if (gameObject is ICharacter character)
            {
                viewers.Add(character);
            }
        }
        return viewers;
    }

    public List<ICharacter> GetPastViewers()
    {
        var viewers = new List<ICharacter>();
        foreach (var objId in pastViewers)
        {
            var gameObject = ObjectTable.SearchById(objId);
            if (gameObject is ICharacter character)
            {
                viewers.Add(character);
            }
        }
        viewers.Add(ClientState.LocalPlayer);
        return viewers;
    }

    // Method to target a character
    public void TargetCharacter(ICharacter character)
    {
        if (character != null)
        {
            TargetManager.Target = character;
        }
    }

    // Method to highlight a character
    public void HighlightCharacter(ICharacter character)
    {
        if (character is IPlayerCharacter charX && charX != null)
        {
            TargetManager.MouseOverTarget = charX;
        }
    }

    // Method to send chat commands
    public void SendChatCommand(string command, ICharacter character)
    {
        if (character is IPlayerCharacter charX && charX != null)
        {
            string name = $"{charX.Name}@{charX.HomeWorld.RowId}";
            command = $"/{command} {name}";    
            ChatGui.Print(command);
        }
    }

    public void OpenChatWith(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            //ChatGui.OpenChat(message, false);
        }
    }


    // Method to copy player's name to clipboard
    public void CopyPlayerName(ICharacter character)
    {
        if (character != null)
        {
            ImGui.SetClipboardText(character.Name.TextValue);
            ChatGui.Print($"Copied {character.Name.TextValue} to clipboard.");
        }
    }

    // Method to open the Adventurer Plate (not possible directly, so we inform the user)
    public void ShowAdventurerPlateInfo(ICharacter character)
    {
        if (character != null)
        {
            // Inform the user that they can search for the player's Adventurer Plate
            ChatGui.Print($"To view {character.Name}'s Adventurer Plate, please use the in-game search.");
        }
    }

    // Expose viewer timestamps to MainWindow
    public Dictionary<ulong, DateTime> GetViewerTimestamps()
    {
        return viewerTimestamps;
    }
    public void OpenAdventurerPlate(ICharacter viewer)
    {
        if (viewer == null)
            return;

        // Get the Agent for Adventurer Plate

        var agent = GameGui.GetAddonByName("Adventure Plate");

        if (agent == IntPtr.Zero)
        {
            // Optionally display a message if the agent is not found
            ChatGui.PrintError("Could not find Adventurer Plate.");
            return;
        }

        // Prepare the data for opening the Adventurer Plate
        var playerObjectId = viewer.GameObjectId;

        // Marshal the function to open the Adventurer Plate
        unsafe
        {
            var agentAdventurerPlate = (AgentAdventurerPlate*)agent;

            if (agentAdventurerPlate != null)
            {
                agentAdventurerPlate->OpenPlate(playerObjectId);
            }
            else
            {
                ChatGui.PrintError("AgentAdventurerPlate is null.");
            }
        }
    }
}

// Define the AgentAdventurerPlate struct
[StructLayout(LayoutKind.Explicit)]
public unsafe struct AgentAdventurerPlate
{
    [FieldOffset(0x0)] public IntPtr vtbl;

    public void OpenPlate(ulong objectId)
    {
        // The function signature may vary; this is an example
        var openPlateDelegate = Marshal.GetDelegateForFunctionPointer<OpenPlateDelegate>(vtbl);
        openPlateDelegate(this, objectId);
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void OpenPlateDelegate(AgentAdventurerPlate agent, ulong objectId);
}

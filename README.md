# PeepingTim

**PeepingTim** is a Dalamud plugin for Final Fantasy XIV that helps you keep track of players who are currently targeting you. It provides additional features for monitoring, interacting with, and quickly contacting these “watchers.”

## Features

- **Live Monitoring:** Displays a list of players who are currently or recently targeting you.
- **Visual Indicators:** Color-coded entries to differentiate between currently active watchers, recently seen observers, and players no longer loaded in your area.
- **Notifications:** Optional sound alert when a new player starts targeting you.
- **Interaction Shortcuts:** Quick actions via context menu, such as:
  - Targeting the character directly
  - Opening a message window (Tell)
  - Viewing their Adventurer Plate (if available)
  - Copying the player's name to the clipboard

## Installation

1. **Prerequisite:** This plugin requires [Dalamud](https://github.com/goatcorp/Dalamud), which comes with the [FFXIVLauncher (Goatcorp)](https://github.com/goatcorp/FFXIVQuickLauncher).
2. **Add the Repository:** If not already done, add this plugin’s GitHub repository to the Dalamud Plugin Installer.
3. **Install the Plugin:** In-game, open the Dalamud Plugins menu (usually by typing **`/xlplugins`**), search for **PeepingTim**, and click **Install**.

## Usage

### Commands

- **`/ptim` or `/peepingtim`**  
  Opens the main window, listing all players who are currently or recently targeting you.

- **`/ptimconfig`**  
  Opens the configuration window for adjusting colors, sounds, and other settings.

#### Additional Interactions

- **Left-click on a player's name:**  
  Attempts to target that player (if loaded).
  
- **Right-click on a player's name:**  
  Opens a context menu with additional actions (e.g., send Tell, view Adventurer Plate).

### Configuration

In the configuration window, you can:

- **Enable/Disable Sound:** Play a sound alert when a new player targets you.
- **Adjust Volume:** Set the loudness of the alert sound.
- **Customize Colors:** Choose colors for active, recently seen, and unloaded observers.

## Known Limitations

- The display depends on data provided by Dalamud about nearby players.
- Players outside the loaded game area are shown as “unloaded.”

## Credits

- **Author:** kcuY  
- **Inspired by:** [Peeping Tom by anna](https://git.anna.lgbt/anna/PeepingTom/src/branch/main/Peeping%20Tom)  
- **Version:** See the plugin’s configuration window for version details
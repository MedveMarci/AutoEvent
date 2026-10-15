# AutoEvent.AutoRun

AutoEvent.AutoRun is an **optional plugin module** that automatically runs AutoEvent mini-games one after another. It
keeps players in a lobby between rounds, waits for enough players, then starts the next mini-game.

---

## Requirements

- **AutoEvent** plugin (main plugin must be installed)
- [AutoEvent.Vote](Vote.md) — *optional*, only required if you enable `UseVote` to let players vote for the next
  mini-game
- [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI) — *optional*, pulled in through AutoEvent.Vote when voting
  is enabled

---

## Installation

1. Download `AutoEvent.AutoRun.dll` from the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest)
2. Place it in:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
3. (Optional) Install `AutoEvent.Vote.dll` and `RadioMenuAPI.dll` if you want the voting feature
4. Grant the AutoRun permission in `permissions.yml`:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml`

```yaml
owner:
  inheritance: []
  permissions:
    - ev.auto
```

---

## Commands

All AutoRun commands are subcommands under `ev auto`.

| Command          | Aliases       | Description                                                          |
|------------------|---------------|----------------------------------------------------------------------|
| `ev auto start`  | `begin`, `on` | Starts the automatic mini-game loop                                  |
| `ev auto stop`   | `end`, `off`  | Stops the loop (and the current mini-game, if one is running)        |
| `ev auto status` | `info`        | Shows loop state, current mini-game, selection mode, pool and voting |

**Example:**

```
ev auto start
ev auto status
ev auto stop
```

**Notes:**

- The loop can also start on its own each round if `StartAutomatically` is `true`.

---

## How It Works

1. The loop enters the **lobby**: players are teleported to `LobbyPosition`.
2. It waits until at least `MinPlayers` eligible players are present, showing the "waiting for players" broadcast.
3. A `LobbyDuration`-second countdown runs.
4. The next mini-game is chosen:
    - **Random** or
    - by a **vote** during the lobby if `UseVote` is enabled and AutoEvent.Vote is installed.
5. The mini-game starts. When it ends, the loop returns to the lobby and repeats.

---

## Server Name Variable (`{event}`)

AutoRun registers an **`{event}`** variable. Put it anywhere in your server's `server_name` or `player_list_title` in
the server config, and the game substitutes the current mini-game name automatically — showing `Lobby` while waiting
between games.

**Example (`config_gameplay.txt` / server config):**

```
server_name: My Server - Now playing: {event}
player_list_title: {event}
```

Because it uses the native formatter, the value refreshes on the game's own schedule and works alongside the built-in
variables like `{player_count}` and `{ip}`. Just include `{event}` in the name.

What `{event}` is replaced with is customizable in `translation.yml`:

- While a mini-game runs it uses **`ServerNameRunning`**, with `{eventName}` replaced by the mini-game's name (e.g.
  `Now playing: {eventName}`).
- While waiting in the lobby it uses **`ServerNameLobby`** (e.g. `Lobby` or `Waiting...`).

---

## Configuration

Configuration lives in the plugin's config folder (`<port>` is the server port, e.g. `7777`):

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\AutoEvent.AutoRun\config.yml
         %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\AutoEvent.AutoRun\translation.yml
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/AutoEvent.AutoRun/config.yml
         ~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/AutoEvent.AutoRun/translation.yml
```

### `config.yml`

| Option               | Default                      | Description                                                                                                      |
|----------------------|------------------------------|------------------------------------------------------------------------------------------------------------------|
| `Debug`              | `false`                      | Enable/disable debug logging                                                                                     |
| `StartAutomatically` | `false`                      | Automatically start the loop when the round starts                                                               |
| `SelectionMode`      | `Random`                     | How the next mini-game is chosen: `Random` or `Sequential`                                                       |
| `AvoidRepeats`       | `true`                       | Don't repeat a mini-game until every other one in the pool has run; refills automatically. Also applies to votes |
| `IncludedEvents`     | `[]` (all)                   | Pool of mini-games by command name (see `ev list`). Empty = every registered mini-game                           |
| `LobbyDuration`      | `15`                         | Seconds players wait in the lobby between mini-games                                                             |
| `MinPlayers`         | `2`                          | Minimum eligible players before a mini-game starts; below this the loop waits                                    |
| `UseVote`            | `false`                      | If true (and AutoEvent.Vote is installed), players vote for the next mini-game during the lobby                  |
| `VoteDuration`       | `30`                         | How long a vote lasts, in seconds (only when `UseVote` is true)                                                  |
| `VoteEventCount`     | `3`                          | How many mini-games are offered in a vote (only when `UseVote` is true)                                          |
| `TeleportToLobby`    | `true`                       | Teleport players to `LobbyPosition` while waiting in the lobby                                                   |
| `LobbyPosition`      | `(39.332, 314.112, -31.922)` | The lobby teleport position (only when `TeleportToLobby` is true)                                                |

### `translation.yml`

| Option                       | Default                                          | Description                                                                 |
|------------------------------|--------------------------------------------------|-----------------------------------------------------------------------------|
| `LobbyBroadcast`             | "The next minigame starts in {seconds} seconds!" | Lobby countdown broadcast (`{seconds}` placeholder)                         |
| `WaitingForPlayersBroadcast` | "Waiting for players to join..."                 | Shown while waiting for enough players                                      |
| `NotEnoughPlayersBroadcast`  | "Not enough players, back to the lobby!"         | Shown when a vote or mini-game is abandoned because only one player is left |
| `LoopStartedBroadcast`       | "Automatic minigames enabled!"                   | Shown when the loop starts                                                  |
| `LoopStoppedBroadcast`       | "Automatic minigames disabled."                  | Shown when the loop stops                                                   |
| `ServerNameRunning`          | "{eventName}"                                    | `{event}` server-name value while running (`{eventName}` = mini-game name)  |
| `ServerNameLobby`            | "Lobby"                                          | `{event}` server-name value while in the lobby                              |

---

## Permissions

Permissions are set in `permissions.yml`:

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml`

```
ev.auto      — Full access to AutoRun commands
```

Example:

```yaml
owner:
  inheritance: []
  permissions:
    - ev.auto

moderator:
  inheritance: []
  permissions:
    - ev.auto
```

---

## Troubleshooting

**"AutoEvent plugin is not loaded!"**

- Install `AutoEvent.dll` and ensure AutoEvent loads first:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`

**The loop waits forever in the lobby**

- You need at least `MinPlayers` eligible players. Lower `MinPlayers` or wait for more players.

**Voting does nothing / falls back to automatic selection**

- Voting requires **AutoEvent.Vote** (and RadioMenuAPI) installed and `UseVote: true`. See [AutoEvent.Vote](Vote.md).

---

## See Also

- [Main AutoEvent Documentation](../README.md)
- [AutoEvent.Vote](Vote.md) — voting system for choosing mini-games
- [Installation Guide](Installation.md)
- [Commands Reference](Commands.md)

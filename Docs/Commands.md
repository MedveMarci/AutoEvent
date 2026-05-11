# Commands

All AutoEvent commands use the `ev` prefix and are entered in the **Remote Admin console**.

---

## Command Reference

| Command                                       | Description                                                              |
|-----------------------------------------------|--------------------------------------------------------------------------|
| `ev list`                                     | Lists all available mini-games with their command names and descriptions |
| `ev run <name>`                               | Starts a mini-game by its command name                                   |
| `ev run <name> <mapName>`                     | Starts a mini-game with a specific map override                          |
| `ev stop`                                     | Stops the currently running mini-game                                    |
| `ev reload`                                   | Reloads configs and translations (cannot run while event is active)      |
| `ev update`                                   | Checks and updates all schematics to the latest versions                 |
| `ev volume <0-200>`                           | Sets the global music volume in percent                                  |
| `ev language list`                            | Lists all available translations                                         |
| `ev language load <name>`                     | Loads a translation by name                                              |
| `ev vote create <duration> <event1> <event2>` | Starts a vote for mini-games (requires AutoEvent.Vote plugin)            |
| `ev vote end <true/false>`                    | Ends the current vote (requires AutoEvent.Vote plugin)                   |

**Aliases for `ev run`:** `start`, `play`, `begin`

---

## Permissions

Permissions are set in `LabApi/configs/permissions.yml`.

```
ev.*              — Full access to all AutoEvent commands
  ev.list         — View available events
  ev.run          — Start an event
  ev.stop         — Stop an event
  ev.reload       — Reload configs and translations
  ev.update       — Update schematics to latest versions
  ev.vote         — Start and end voting (requires AutoEvent.Vote plugin)
  ev.volume       — Change music volume
  ev.language     — Change language/translation
```

Example configuration:

```yaml
owner:
  inheritance: []
  permissions:
    - ev.*
```

---

## Usage Examples

### Listing available mini-games

```
ev list
```

Output shows all available events in the format `[commandname] Event Name — Description`.

---

### Starting a mini-game

```
ev run lava
ev run amongus
ev run cs
```

Start with a specific map override:

```
ev run airstrike DeathParty
```

---

### Stopping a mini-game

```
ev stop
```

Stops the running event, teleports all players back to the lobby, and cleans up the map.

---

### Changing volume

```
ev volume 50
ev volume 100
ev volume 0
```

Range: `0` (muted) to `200` (maximum). The setting is saved to config.

---

### Changing language

```
ev language list
ev language load english
ev language load russian
```

The translation replaces the current one. A server restart is required for changes to take full effect.

---

### Updating schematics

```
ev update
```

Checks all schematic files for updates and downloads the latest versions. This command:

- **Must be run after first installation** to download the required schematics
- Cannot be run while a mini-game is running
- Updates both ProjectMER and TME (ThousandMuse Editor) schematic formats
- Shows how many schematics were updated and how many failed (if any)
- All schematics are up to date if no updates are available

Requires permission: `ev.update`

---

### Voting System (AutoEvent.Vote plugin)

**Note:** These commands require the optional [AutoEvent.Vote](Vote.md) plugin to be installed.

#### Create a vote

```
ev vote create 30 lava dodgeball cs
ev vote create 20 airstrike glass tag
```

Starts a voting session for mini-games:

- `<duration>` — voting time in seconds
- `<event1> <event2>` — command names of events to choose from
- Opens a radio menu for all ready players
- Cannot start if an event is already running
- Cannot start if another vote is already active

Requires permission: `ev.vote`

#### End a vote

```
ev vote end true
ev vote end false
```

Ends the current vote immediately:

- `true` — starts the winning event after vote ends
- `false` — only counts votes, doesn't start an event
- If tie or no votes: displays message but doesn't start event

Requires permission: `ev.vote`

For full documentation, see [AutoEvent.Vote](Vote.md)

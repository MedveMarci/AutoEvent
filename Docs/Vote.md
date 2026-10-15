# AutoEvent.Vote

AutoEvent.Vote is an **optional plugin module** that adds a voting system to AutoEvent, allowing players to vote on
which mini-game to play next using a radio menu interface.

---

## Requirements

- **AutoEvent** plugin (main plugin must be installed)
- [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI) **1.4.0 or newer** — required for the radio menu voting
  interface

---

## Installation

1. Download `AutoEvent.Vote.dll` from the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest)
2. Place it in:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
3. Ensure **RadioMenuAPI** is installed in the same folder
4. Grant voting permission in `permissions.yml`:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml`

```yaml
owner:
  inheritance: []
  permissions:
    - ev.vote
```

---

## Commands

All Vote commands are subcommands under `ev vote`.

### Start a Vote

```
ev vote create <duration> <event1> <event2> [event3...]
```

**Parameters:**

- `<duration>` — voting time in seconds (e.g., `30`)
- `<event1> <event2> ...` — command names of events to include in the vote (e.g., `lava`, `dodgeball`, `cs`)

**Example:**

```
ev vote create 30 lava dodgeball cs
ev vote create 20 airstrike glass tag
```

**Behavior:**

- Opens a radio menu on all ready players
- Shows voting countdown on screen
- Prevents vote if an event is already running
- Cannot start multiple votes simultaneously

### End a Vote

```
ev vote end <true/false>
```

**Parameters:**

- `<true>` — starts the winning event after vote ends
- `<false>` — only counts votes, doesn't start an event

**Example:**

```
ev vote end true
ev vote end false
```

**Behavior:**

- Ends the current vote immediately
- Counts votes and determines winner
- If tie: displays tied events without starting any
- If no votes: displays message
- Removes radio items from all players

---

## How It Works

1. Admin runs `ev vote create 30 lava dodgeball cs`
2. All ready players receive a radio menu with event options
3. Players select their preferred event via radio menu
4. 30-second countdown broadcasts to all players
5. When timer expires, votes are tallied
6. Event with most votes starts automatically (if `true` flag used)
7. If tie or no winner found, no event starts

---

## Voting Results

**Broadcast Messages** (configurable in `translation.yml`):

- **Vote Started:** Shows remaining time during voting countdown
- **Vote Ended (Winner):** Shows the winning event and its votes — the event starts automatically
- **Vote Ended (Tie):** Lists the tied events — no event starts
- **Vote Ended (No Votes):** Message displayed when no one voted
- **Vote Ended (By Staff):** Displayed when `ev vote end false` is used

Players can put the radio away and take it out again: the menu opens on the item they highlighted last, and that choice
is counted even if the radio isn't in their hand when the vote ends.

---

## Configuration

The texts are stored in (`<port>` is the server port, e.g. `7777`):

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\AutoEvent.Vote\translation.yml
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/AutoEvent.Vote/translation.yml
```

The file is created in the language set in AutoEvent (`language` in AutoEvent's config) and is rewritten by
`ev language load <language>`. AutoEvent.Vote ships the same languages as AutoEvent.

```yaml
menu_title: Vote for a minigame!
broadcast_text: |-
  <b>Vote for the next minigame with your radio!</b>
  <size=25>Change your choice with Left Mouse Click. {duration} seconds remaining.</size>
ended_with_no_vote: <b>The vote has ended with no votes cast.</b>
ended_with_tie: |-
  <b>The vote has ended in a tie between:</b>
  {events}
ended_with_winner: |-
  <b>The vote has ended!</b>
  The winner is <color=yellow>{event}</color> with {votes} vote(s).
ended_by_staff: <b>The vote was ended by a staff member.</b>
```

| Option               | Placeholders         | Description                                   |
|----------------------|----------------------|-----------------------------------------------|
| `menu_title`         |                      | Title shown in the radio voting menu          |
| `broadcast_text`     | `{duration}`         | Countdown message                             |
| `ended_with_no_vote` |                      | Message when no one participates in the vote  |
| `ended_with_tie`     | `{events}`           | Message when multiple events have equal votes |
| `ended_with_winner`  | `{event}`, `{votes}` | Message when the vote has a winner            |
| `ended_by_staff`     |                      | Message when staff ends the vote with `false` |

The `config.yml` of AutoEvent.Vote only contains `debug`:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\AutoEvent.Vote\config.yml
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/AutoEvent.Vote/config.yml
```

---

## Permissions

Permissions are set in `permissions.yml`:

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml`

```
ev.vote      — Full access to vote commands
```

Example:

```yaml
owner:
  inheritance: []
  permissions:
    - ev.vote

moderator:
  inheritance: []
  permissions:
    - ev.vote
```

---

## Troubleshooting

**"RadioMenuAPI plugin is not loaded!"**

- Install RadioMenuAPI in the plugins folder:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
- Verify the plugin loads before AutoEvent.Vote

**"AutoEvent plugin is not loaded!"**

- Install AutoEvent.dll in the plugins folder:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
- Ensure AutoEvent loads first

**Vote menu not appearing**

- Verify all players have `ready` status
- Check RadioMenuAPI is working with other radio features
- Check server logs for errors

**"You can't start a vote when an event is in progress"**

- Stop the current event first with `ev stop`
- Then run `ev vote create ...`

**"No valid events were provided"**

- Check event names with `ev list`
- Use command names (in square brackets), not display names
- Example: `ev vote create 30 [lava] [dm]` — use `lava` and `dm`, not "The Floor is LAVA"

---

## See Also

- [Main AutoEvent Documentation](../README.md)
- [AutoEvent.AutoRun](AutoRun.md) — run mini-games automatically one after another
- [Installation Guide](Installation.md)
- [Commands Reference](Commands.md)
- [PluginApi Reference](PluginApi.md)

# AutoEvent.Vote

AutoEvent.Vote is an **optional plugin module** that adds a voting system to AutoEvent, allowing players to vote on
which mini-game to play next using a radio menu interface.

---

## Requirements

- **AutoEvent** plugin (main plugin must be installed)
- [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI) — required for the radio menu voting interface

---

## Installation

1. Download `AutoEvent.Vote.dll` from the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest)
2. Place it in `LabApi/plugins/global/`
3. Ensure **RadioMenuAPI** is installed in `LabApi/plugins/global/`
4. Grant voting permission in `LabApi/configs/permissions.yml`:

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

**Broadcast Messages** (configurable in `configs.yml`):

- **Vote Started:** Shows remaining time during voting countdown
- **Vote Ended (Winner):** "The winning event is: [Event Name]" — event starts automatically
- **Vote Ended (Tie):** "Multiple events are tied: [Event1, Event2, ...]" — no event starts
- **Vote Ended (No Votes):** Message displayed when no one voted
- **Vote Ended (By Staff):** Displayed when `ev vote end false` is used

---

## Configuration

Configuration is stored in:

```
LabApi/configs/global/AutoEvent/configs.yml
```

**AutoEvent.Vote Config Options:**

```yaml
autoevent_vote:
  MenuTitle: "Vote for the next event"
  BroadcastText: "Vote is active! Time remaining: {duration}s"
  EndedWithWinner: "The winning event is: "
  EndedWithTie: "Multiple events are tied: "
  EndedWithNoVote: "No one voted!"
  EndedButEventNotFound: "The winning event '{winningEvent}' was not found."
  EndedByStaff: "The vote was ended by staff."
```

| Option                  | Default Value                                       | Description                                                                 |
|-------------------------|-----------------------------------------------------|-----------------------------------------------------------------------------|
| `MenuTitle`             | "Vote for the next event"                           | Title shown in the radio voting menu                                        |
| `BroadcastText`         | "Vote is active! Time remaining: {duration}s"       | Countdown message (use `{duration}` placeholder)                            |
| `EndedWithWinner`       | "The winning event is: "                            | Message when vote has a winner                                              |
| `EndedWithTie`          | "Multiple events are tied: "                        | Message when multiple events have equal votes                               |
| `EndedWithNoVote`       | "No one voted!"                                     | Message when no one participates in vote                                    |
| `EndedButEventNotFound` | "The winning event '{winningEvent}' was not found." | Message when winning event doesn't exist (use `{winningEvent}` placeholder) |
| `EndedByStaff`          | "The vote was ended by staff."                      | Message when staff ends vote with `false` flag                              |

---

## Permissions

Permissions are set in `LabApi/configs/permissions.yml`.

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

- Install RadioMenuAPI in `LabApi/plugins/global/`
- Verify the plugin loads before AutoEvent.Vote

**"AutoEvent plugin is not loaded!"**

- Install AutoEvent.dll in `LabApi/plugins/global/`
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
- [Installation Guide](Installation.md)
- [Commands Reference](Commands.md)
- [PluginApi Reference](PluginApi.md)

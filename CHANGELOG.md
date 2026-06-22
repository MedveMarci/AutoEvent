# Changelog

## [10.1.2]

### Added
- Load-time dependency check: when the plugin loads, it now reports every dependency in one clear block.

### Fixed
- Fixed NullReferenceException when trying to use the `ev` command if the plugin isn't fully loaded. (In AutoEvent.Vote also)
- Overhauled Friendly Fire systems in AutoEvent. It should fix the CedMod FF Detector issue. 
- Event coroutine:
  - Stopping an event now also kills the outer timing coroutine that sequences the countdown/game/post-round phases;
    it was previously left running.
  - Cleanup can no longer run twice.
  - `StopEvent()` is now runs once.


## [10.1.1]

### Fixes
- Among Us:
  - Fixed VisualTasks config option.
  - Fixed a permanent stuck bug.
- Fixed waves are not unpausing when the event ends.


## [10.1.0]

### Added

- Added the original plugin's link to README.md.
- Added `sl_egypt` DeathRun map by PresidentFinny.
  - You can start it with `ev run deathrun sl_egypt` after you re-generate your config file or add the map manually.

### Changed

- The `ev language load` can load languages by code or name, which doesn't require the full name just a part of it.
- Updated to the latest BearmanAPI, which includes automatic error sharing.
- Waves are now paused with `IsForcefullyPaused` instead of blocking the event.
- Decontamination is now disabled instead of blocking the event.
- DeathRun:
  - Changed `Spawnpoint` and `Spawnpoint1` to `RunSpawn` and `DeathSpawn` in the DeathRun Temple map (v1.0.1).
  - Changed `RoundDurationInSeconds` to 530. (Make sure you update it)

### Fixed

- CedMod is no longer a hard dependency: its FF autoban is now toggled via reflection, so AutoEvent works with or
  without CedMod installed.
- Fixed seasonal map selection picking an empty map pool when no season-less maps existed.
- Map/loadout/role weighted random selection is now statistically correct (weights were previously re-rolled per item).
- Loadouts with `NoReloadInfiniteAmmo` no longer incorrectly receive regular `InfiniteAmmo`.
- DeadMan Switch is disabled if an event starts preventing it from starting.
- Fixed README.md's PluginApi link.
- Added Null checks for event managers, translations and commands.
- Spawn Protection is disabled while running the events.
- Fixed `ev run` command when using in Lobby.
- Fixed CedMod and Auto FF ban.
  - If it not disables it again, make an issue in Discord or in Github.
- Among Us:
  - SilentWalk getting removed from Impostors when exiting a vent.
  - Impostors permanently lost the sabotage menu when a sabotage lasted longer than the sabotage cooldown.
  - A late-joining (non-participant) player pressing the meeting button could freeze the game in a meeting that never
    started.
  - Body reports no longer remove SilentWalk from everyone; meetings now consistently skip dead players when
    teleporting.
  - Meetings now pull players out of vents (vented state and Lightweight are cleared).
  - Using a custom map name in the config no longer crashes task/sabotage generation (falls back to the Skeld set).
  - The MedBay scan (Submit Scan) task is now actually assigned to crewmates - previously the scanner existed but the
    task was never generated.
  - The MedBay scan no longer locks a player in place if they die mid-scan.
- Zombie Survival:
  - The end sound is now played correctly when the event ends.
  - The main music will stop correctly.
- Simon's Prison:
  - Fixed item disappearing with Jailers.

### Removed

- Removed AudioPlayerAPI support.
# Changelog

## [10.2.0]

### Added

- New add-on: **AutoEvent.AutoRun** — runs mini-games automatically one after another, with a lobby countdown.
- Translation support for AutoEvent.AutoRun and AutoEvent.Vote (`translation.yml` in their config folders), in all 12
  languages of AutoEvent.
- `ev language load` now changes the language of the add-ons too, and the chosen language is saved as `language` in
  AutoEvent's config. Missing add-on translation files are created in that language.
- `AllowDroppingItem` event flag: players can only drop items in mini-games that set it.
- `SpawnProtectionDuration` config option for GunGame.
- The music is built into `AutoEvent.dll`, `Music.zip` is no longer needed. On start, missing files are saved to the
  `Music` folder (existing files are never overwritten, so custom music is kept); if a file is missing anyway, the
  built-in copy is played from the DLL.

### Changed

- Rewritten Hungarian translation.
- Translation updates.
- Redesigned `ev` help in the Remote Admin: colored sections for the mini-game commands and for each add-on (AutoRun,
  Vote) with usage and aliases; it also shows when an add-on isn't installed. `ev auto` and
  `ev vote` use the same layout.
- The Floor is LAVA: picked up weapons are loaded and can be fired right away (no reload needed), and they can be
  dropped.
- Cock Fights (Versus): a player who reached `JailbirdLifespan` is no longer killed. They are taken out of the arena,
  get a hint about it and have to sit out one round. The limit (and its hints) doesn't apply to the last player of a
  team. Players also get a hint about how many rounds they can still play in a row.
- AutoEvent.Vote **2.0.0** (breaking: new RadioMenuAPI requirement and the texts moved to `translation.yml`):
    - The selected option is kept when the radio is put away and taken out again, and it is counted even if the radio
      isn't in the player's hand when the vote ends. **Requires RadioMenuAPI 1.4.0**.
    - The vote radio can't be dropped while the vote is running, and it's taken away (not dropped) when the player dies
      or disconnects.
    - The texts moved from `config.yml` to `translation.yml` (**re-check your texts after updating**).
    - Players in `IgnoredRoles` (e.g. Overwatch) no longer get a vote radio.
- Version check: the add-ons use the same `check-update` endpoint as AutoEvent.
- `ev volume` accepts `50`, `50%` or `50,5`, clamps it to 0-200 and works without a running mini-game.
- Re-formatted the whole solution.

### Fixed

- Vote: a vote that ended while a mini-game was already running started a second one.
- Vote: a round restart during a vote left it "running" forever.
- `ev run` started from the lobby could start a second mini-game if another one started during the 1 second delay.
- `ev reload` overwrote the server name when AutoEvent wasn't loaded.
- An empty `name`/`command_name` in `translation.yml` made a mini-game impossible to start.
- The ProjectMER schematics path patch could error on Harmony-generated stack frames.
- Battle: players now start with their weapon in hand.
- Team Death-Match: wrong winner when a whole team left; a 0 kill goal caused an error.
- GunGame: errors with players who joined during the game, and a random winner (or an error) when the time ran out.
- Death Line: spectators were counted when picking the winner.
- All Deathmatch: error at the end for players who joined late.
- FallDown: an already warned platform could be picked again.
- Dodgeball, Football, Jail, Survival: far fewer broadcast per frame.

### Removed

- The `ClientDropItem` Harmony patch (replaced by the `AllowDroppingItem` flag).
- The unused `reload` text of The Floor is LAVA.

## [10.1.3]

## Fixed

- Fixed server crash when the BearmanAPI is down.
- Fixed several issues with `sl_egypt` map.
- Fixed Among Us MedBay Scan task.
- Fixed Among Us voting.
- Fixed CleanUp when stopping an event while it's finished.
- Spectators are no longer pulled back into a running mini-game by the respawn waves.
- Friendly Fire is now only force-enabled in mini-games that mechanically require it (All Deathmatch, Gun Game, Among
  Us, Lava, Jail, Airstrike). Team/duel games (Team Death-Match, Counter-Strike, Versus, Knives, Battle and Tag) no
  longer force a value, so the per-event `EnableFriendlyFire` config now controls them.

## [10.1.2]

### Added

- Load-time dependency check: when the plugin loads, it now reports every dependency in one clear block.

### Fixed

- Fixed NullReferenceException when trying to use the `ev` command if the plugin isn't fully loaded. (In AutoEvent.Vote
  also)
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
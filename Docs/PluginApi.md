# AutoEvent Plugin API

This document describes how to create custom mini-games for AutoEvent and register them from your own plugin.

---

## Overview

AutoEvent provides a clean API for building mini-games. Each event is a self-contained class that inherits from the
`Event<TConfig, TTranslation>` base class and optionally implements interfaces for map and audio support.

**Key capabilities:**

- Register custom events from external plugins
- Full event lifecycle management (startup, countdown, game loop, cleanup)
- YAML-based config and multi-language translation support
- Map loading via ProjectMER schematics
- Background music via SecretLabNAudio or AudioPlayerAPI (determined by which DLL variant is installed)
- Loadout system for role, item, health, and effect assignment

---

## Creating a Custom Event

### 1. Basic Structure

```csharp
using AutoEvent.API;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using PlayerRoles;

public class MyEvent : Event<MyConfig, MyTranslation>
{
    public override string Name { get; set; } = "My Custom Event";
    public override string Description { get; set; } = "A fun custom game mode";
    public override string Author { get; set; } = "YourName";
    public override string CommandName { get; set; } = "myevent";

    protected override void OnStart()
    {
        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.DefaultLoadout);
        }
    }

    protected override void ProcessFrame()
    {
        // Called every FrameDelayInSeconds (default: 1 second)
        // Update broadcasts, check conditions, etc.
    }

    protected override bool IsRoundDone()
    {
        return Player.ReadyList.Count < 2;
    }

    protected override void OnFinished()
    {
        Extensions.ServerBroadcast("Event finished!", 10);
    }
}

public class MyConfig : EventConfig
{
    public Loadout DefaultLoadout { get; set; } = new()
    {
        Roles = new() { { RoleTypeId.NtfSpecialist, 1 } },
        Items = new() { ItemType.ArmorCombat, ItemType.Medkit }
    };
}

public class MyTranslation : EventTranslation { }
```

### 2. Adding Map Support

Implement `IEventMap` to automatically spawn a schematic via ProjectMER:

```csharp
using AutoEvent.Interfaces;
using UnityEngine;

public class MyEvent : Event<MyConfig, MyTranslation>, IEventMap
{
    public MapInfo MapInfo { get; set; } = new(
        schematicName: "MySchematic",
        position: new Vector3(0, 1030, 0),
        rotation: Vector3.zero,
        scale: Vector3.one
    );
}
```

The map is spawned before `OnStart()` and despawned during cleanup automatically.
Uses [ProjectMER](https://github.com/Michal78900/ProjectMER) for schematic loading.

### 3. Adding Audio Support

Implement `IEventSound` to automatically play background music:

```csharp
using AutoEvent.Interfaces;

public class MyEvent : Event<MyConfig, MyTranslation>, IEventSound
{
    public SoundInfo SoundInfo { get; set; } = new(
        name: "background_music.ogg",
        volume: 25,
        loop: true
    );
}
```

Audio starts after the countdown and stops during cleanup automatically.

### 4. Hooking Into Game Events

Override `RegisterEvents()` and `UnregisterEvents()`:

```csharp
using LabApi.Events.CustomHandlers;

protected override void RegisterEvents()
{
    ServerEventHandlers.PlayerDied += OnPlayerDied;
    ServerEventHandlers.PlayerHurt += OnPlayerHurt;
}

protected override void UnregisterEvents()
{
    ServerEventHandlers.PlayerDied -= OnPlayerDied;
    ServerEventHandlers.PlayerHurt -= OnPlayerHurt;
}

private void OnPlayerDied(PlayerDiedEventArgs ev)
{
    // Custom logic
}
```

### 5. Limiting Player Count

Implement `IPlayerCountLimited` to enforce a maximum:

```csharp
using AutoEvent.Interfaces;

public class MyEvent : Event<MyConfig, MyTranslation>, IPlayerCountLimited
{
    public int MaxPlayers { get; set; } = 10;
}
```

---

## Registering Events From Your Plugin

### Register

In your plugin's `Enable()` method using the public `EventManager` API:

```csharp
using AutoEvent.API;
using AutoEvent.API.Enums;

public override void Enable()
{
    _myEvent = new MyEvent();
    var result = EventManager.RegisterEvent(_myEvent);

    if (result != EventRegistrationResult.Success)
        Logger.Warn($"Failed to register event: {result}");
}
```

### Unregister

In your plugin's `Disable()` method:

```csharp
public override void Disable()
{
    EventManager.UnregisterEvent(_myEvent);
    // Or unregister by command name:
    // EventManager.UnregisterEvent("myevent");
    _myEvent = null;
}
```

### Query Events

Check if an event is registered or retrieve it:

```csharp
// Check if registered
if (EventManager.IsRegistered("myevent"))
{
    // Event exists
}

// Get event by command name or full name
var ev = EventManager.GetEvent("myevent");

// Safe lookup
if (EventManager.TryGetEvent("myevent", out var myEvent))
{
    // Use myEvent
}

// Get current running event
var currentEvent = EventManager.CurrentEvent;

// Get all registered events
var allEvents = EventManager.Events;

// Check if ProjectMER is loaded
if (EventManager.IsMerLoaded)
{
    // Can use IEventMap events
}
```

### Registration Results

| Result                     | Meaning                                            |
|----------------------------|----------------------------------------------------|
| `Success`                  | Event registered successfully                      |
| `EventIsNull`              | The event instance was null                        |
| `AlreadyRegistered`        | An event with the same command name already exists |
| `NotFound`                 | (Unregister only) Event not found in registry      |
| `CannotUnregisterInternal` | Cannot unregister built-in AutoEvent events        |
| `MissingProjectMer`        | Event requires maps but ProjectMER is not loaded   |

---

## Event Lifecycle

```
StartEvent(mapName?)
  ↓
SetMap()              — Select random map from config
SpawnMap()            — Load schematic (if IEventMap)
RegisterEvents()      — Hook into game events
OnStart()             — Your initialization logic
StartAudio()          — Play background music (if IEventSound)
  ↓
BroadcastStartCountdown()   — Countdown coroutine
CountdownFinished()         — Called after countdown completes
  ↓
RunGameCoroutine()
  while (!IsRoundDone())
    ProcessFrame()           — Called every FrameDelayInSeconds
  OnFinished()               — Called when round ends
  ↓
[PostRoundDelay seconds wait]
  ↓
OnInternalCleanup()
  UnregisterEvents()
  DeSpawnMap()
  StopAudio()
  CleanUpAll()         — Remove items and ragdolls
  TeleportEnd()        — Return players to lobby
  OnCleanup()          — Your custom cleanup logic
```

---

## Configuration & Translation

AutoEvent automatically generates YAML files for your event.

**Config file** (`configs.yml`):

```yaml
myevent:
  DefaultLoadout:
    Chance: 1
    Roles:
      NtfSpecialist: 1
    Items:
      - ArmorCombat
      - Medkit
    Health: 0
    Stamina: 0
    InfiniteAmmo: None
  AvailableMaps: []
  AvailableSounds: []
  EnableFriendlyFire: Default
  EnableFriendlyFireAutoban: Default
```

**Translation file** (`translation.yml`):

```yaml
myevent:
  Name: "My Custom Event"
  Description: "A fun custom game mode"
  CommandName: "myevent"
```

---

## Utility API

### Loadout System

```csharp
// Apply a single loadout
player.GiveLoadout(Config.DefaultLoadout);

// Apply a randomly selected loadout from a weighted list
player.GiveLoadout(Config.AllLoadouts);

// Apply with flags to skip certain aspects
player.GiveLoadout(Config.DefaultLoadout,
    LoadoutFlags.IgnoreItems | LoadoutFlags.IgnoreHealth);
```

**LoadoutFlags:**

| Flag                    | Description                       |
|-------------------------|-----------------------------------|
| `None`                  | No flags                          |
| `IgnoreRole`            | Do not change the player's role   |
| `IgnoreItems`           | Do not give items                 |
| `DontClearDefaultItems` | Keep default spawn items          |
| `IgnoreEffects`         | Do not apply effects              |
| `IgnoreHealth`          | Do not set health                 |
| `IgnoreAhp`             | Do not set AHP                    |
| `IgnoreSize`            | Do not change player size         |
| `IgnoreInfiniteAmmo`    | Do not change ammo mode           |
| `ForceInfiniteAmmo`     | Force infinite ammo on            |
| `IgnoreGodMode`         | Do not change god mode            |
| `IgnoreWeapons`         | Do not modify weapon attachments  |
| `IgnoreStamina`         | Do not change stamina             |
| `ForceEndlessClip`      | Enable endless clip               |
| `UseDefaultSpawnPoint`  | Use role's default spawn position |

### Audio

```csharp
// Global audio
var audio = Extensions.PlayAudio("background.ogg", volume: 25, loop: true);
audio.PauseAudio();
audio.ResumeAudio();
audio.StopAudio();

// Per-player audio
Extensions.PlayPlayerAudio(player, "notification.ogg");
```

### Broadcasts

```csharp
// All players
Extensions.ServerBroadcast("Message to everyone", time: 5);

// Single player
player.Broadcast("Personal message", time: 3);
```

### Grenades

```csharp
Extensions.GrenadeSpawn(
    pos: transform.position,
    scale: 1f,
    fuseTime: 3f,
    radius: 15f
);
```

### Map Utilities

```csharp
// Manual map load
var map = Extensions.LoadMap(
    mapInfo.MapName,
    mapInfo.Position,
    mapInfo.MapRotation,
    mapInfo.Scale
);

// Manual map unload
Extensions.UnLoadMap(map);
```

### Cleanup

```csharp
Extensions.CleanUpAll();   // Remove all items and ragdolls
Extensions.TeleportEnd();  // Reset all players to lobby
```

---

## EventFlags

Control which global event behaviors are suppressed during your event:

```csharp
public override EventFlags EventHandlerSettings { get; set; } =
    EventFlags.IgnoreRagdoll | EventFlags.IgnoreBulletHole;
```

| Flag                  | Description                           |
|-----------------------|---------------------------------------|
| `Default`             | No suppression                        |
| `IgnoreBulletHole`    | Suppress bullet-hole decals           |
| `IgnoreRagdoll`       | Prevent ragdoll spawning              |
| `IgnoreDroppingAmmo`  | Prevent ammo drops on death           |
| `IgnoreDroppingItem`  | Prevent item drops on death           |
| `IgnoreHandcuffing`   | Disable handcuffing                   |
| `IgnoreBloodDecal`    | Suppress blood decals                 |
| `IgnorePickingUpItem` | Prevent players from picking up items |

---

## Friendly Fire Control

```csharp
protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } =
    FriendlyFireSettings.Enable;

protected override FriendlyFireSettings ForceEnableFriendlyFireAutoban { get; set; } =
    FriendlyFireSettings.Disable;
```

| Value     | Description             |
|-----------|-------------------------|
| `Default` | Use server setting      |
| `Enable`  | Force friendly fire on  |
| `Disable` | Force friendly fire off |

---

## Custom Countdown

```csharp
protected override IEnumerator<float> BroadcastStartCountdown()
{
    for (int i = 10; i > 0; i--)
    {
        Extensions.ServerBroadcast($"Starting in {i}...", 1);
        yield return Timing.WaitForSeconds(1f);
    }
}

protected override void CountdownFinished()
{
    Extensions.ServerBroadcast("Go!", 3);
}
```

---

## Complete Example

```csharp
using System.Collections.Generic;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

public class HotPotatoEvent : Event<HotPotatoConfig, HotPotatoTranslation>, IEventMap, IEventSound
{
    public override string Name { get; set; } = "Hot Potato";
    public override string Description { get; set; } = "Pass the potato or explode";
    public override string Author { get; set; } = "YourName";
    public override string CommandName { get; set; } = "potato";

    public MapInfo MapInfo { get; set; } = new("MyMap", new Vector3(0, 1030, 0));
    public SoundInfo SoundInfo { get; set; } = new("HotPotato.ogg", 25, true);

    private Player _currentHolder;
    private float _timeLeft = 30f;

    protected override void OnStart()
    {
        foreach (var player in Player.ReadyList)
            player.GiveLoadout(Config.DefaultLoadout);

        _currentHolder = Player.ReadyList[0];
        _timeLeft = 30f;
    }

    protected override void RegisterEvents()
    {
        ServerEventHandlers.PlayerDied += OnPlayerDied;
    }

    protected override void UnregisterEvents()
    {
        ServerEventHandlers.PlayerDied -= OnPlayerDied;
    }

    protected override void ProcessFrame()
    {
        _timeLeft -= FrameDelayInSeconds;

        if (_timeLeft <= 0f && _currentHolder != null)
            _currentHolder.Health = 0;

        Extensions.ServerBroadcast(
            $"Holder: {_currentHolder?.Nickname ?? "None"} — {_timeLeft:F0}s", 1);
    }

    protected override bool IsRoundDone()
    {
        return Player.ReadyList.Count < 2;
    }

    protected override void OnFinished()
    {
        var winner = Player.ReadyList.Count == 1 ? Player.ReadyList[0].Nickname : "Nobody";
        Extensions.ServerBroadcast($"{winner} wins!", 10);
    }

    private void OnPlayerDied(PlayerDiedEventArgs ev)
    {
        if (ev.Player == _currentHolder && Player.ReadyList.Count > 0)
        {
            _currentHolder = Player.ReadyList[0];
            _timeLeft = 30f;
            _currentHolder.Broadcast("You have the potato!", 3);
        }
    }
}

public class HotPotatoConfig : EventConfig
{
    public Loadout DefaultLoadout { get; set; } = new()
    {
        Roles = new() { { RoleTypeId.ClassD, 1 } }
    };
}

public class HotPotatoTranslation : EventTranslation { }
```

---

## Troubleshooting

**Event won't register:**

- `EventIsNull` — pass a valid event instance
- `AlreadyRegistered` — another event uses the same command name
- `MissingProjectMer` — event uses `IEventMap` but ProjectMER is not installed

**Event doesn't appear in `ev list`:**

- Confirm `RegisterEvent()` returned `Success`
- Check server logs for exceptions during registration

**Players not cleaned up after event:**

- Cleanup runs automatically: `UnregisterEvents()` → `DeSpawnMap()` → `StopAudio()` → `CleanUpAll()` → `TeleportEnd()` →
  `OnCleanup()`
- Check server logs for exceptions during cleanup

**Config not loading:**

- Verify YAML syntax in `configs.yml`
- Run `ev reload` after editing the file
- Config file path: `LabApi/configs/global/AutoEvent/configs.yml`

---

## Key Types Reference

| Type                           | Description                                      |
|--------------------------------|--------------------------------------------------|
| `Event<TConfig, TTranslation>` | Base class for all events                        |
| `EventManager`                 | Manages event registration                       |
| `EventRegistrationResult`      | Result enum from register/unregister             |
| `EventConfig`                  | Base class for event configuration               |
| `EventTranslation`             | Base class for event translations                |
| `EventFlags`                   | Bit flags for global handler suppression         |
| `Loadout`                      | Defines role, items, health, effects for players |
| `LoadoutFlags`                 | Controls which aspects of a loadout to apply     |
| `MapInfo`                      | Schematic name, position, rotation, scale        |
| `SoundInfo`                    | Audio filename, volume, loop setting             |
| `IEventMap`                    | Interface for events that use a map              |
| `IEventSound`                  | Interface for events that use background music   |
| `IPlayerCountLimited`          | Interface to enforce player count limits         |
| `Extensions`                   | Static utility methods                           |

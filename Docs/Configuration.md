# Configuration

AutoEvent configuration files are located at:

```
LabApi/configs/global/AutoEvent/
  configs.yml      — Event-specific settings
  translation.yml  — Display names and descriptions
```

Use `ev reload` in the Remote Admin console to reload configs without restarting the server. Reload cannot be run while
an event is active.

---

## Shared Config Options

Every event config includes these shared options:

| Option                         | Description                                                                                        |
|--------------------------------|----------------------------------------------------------------------------------------------------|
| `available_maps`               | List of schematics the event can use. If multiple are listed, one is selected randomly by chance.  |
| `available_sounds`             | List of audio files the event can use. If multiple are listed, one is selected randomly by chance. |
| `enable_friendly_fire`         | Override friendly fire for this event: `Default`, `Enable`, or `Disable`.                          |
| `enable_friendly_fire_autoban` | Override friendly fire autoban for this event: `Default`, `Enable`, or `Disable`.                  |

> Some events force friendly fire on or off regardless of this setting when it is required for gameplay.

---

## Available Maps

Specifies one or more schematics the event can load. If multiple maps are listed, one is selected based on the `chance`
value.

```yaml
available_maps:
  - chance: 100
    map:
      map_name: 'DeathParty'
      position:
        x: 10
        y: 1012
        z: -40
      rotation:
        x: 0
        y: 0
        z: 0
      scale:
        x: 1
        y: 1
        z: 1
      is_static: true
    season_flag: None
```

| Field         | Description                                                     |
|---------------|-----------------------------------------------------------------|
| `chance`      | Relative chance this map is selected when multiple are listed   |
| `map_name`    | Name of the schematic folder inside `AutoEvent/Schematics/`     |
| `position`    | X/Y/Z world coordinates where the map spawns                    |
| `rotation`    | X/Y/Z rotation of the map                                       |
| `scale`       | X/Y/Z scale of the map                                          |
| `is_static`   | Set to `false` if the map contains animated objects             |
| `season_flag` | Restrict this map to a season: `None`, `Christmas`, `Halloween` |

---

## Available Sounds

Specifies one or more audio files the event can play. If multiple are listed, one is selected based on the `chance`
value.

> If you add a sound to the config, the event's default music will not play. To replace the default music, use the same
> filename.

```yaml
available_sounds:
  - chance: 100
    sound:
      sound_name: 'YourMusic.ogg'
      volume: 25
      loop: true
```

| Field        | Description                                                     |
|--------------|-----------------------------------------------------------------|
| `chance`     | Relative chance this sound is selected when multiple are listed |
| `sound_name` | Filename inside `AutoEvent/Music/`                              |
| `volume`     | Playback volume (recommended: 15–40 to comply with VSR 8.3.1)   |
| `loop`       | Whether the audio restarts when it ends                         |

### Converting audio to .ogg format

AutoEvent requires audio in `.ogg` format with the following settings:

1. Go to [convertio.co/mp3-ogg](https://convertio.co/mp3-ogg/)
2. Click **Advanced** and set:
    - Codec: `Ogg (Vorbis)`
    - Quality: Lowest
    - Audio Channels: `Mono (1.0)`
    - Frequency: `48000 Hz`
    - Volume: No change
3. Convert and download the file
4. Place it in `LabApi/configs/AutoEvent/Music/`

---

## FriendlyFireSettings

| Value     | Description                                  |
|-----------|----------------------------------------------|
| `Default` | Use whatever the server's current setting is |
| `Enable`  | Force friendly fire on for this event        |
| `Disable` | Force friendly fire off for this event       |

This setting also applies to `enable_friendly_fire_autoban`.

> This feature is compatible with CedMod.

---

## Example Full Config

```yaml
lava:
  available_maps:
    - chance: 100
      map:
        map_name: 'Lava'
        position:
          x: 0
          y: 1012
          z: 0
        rotation:
          x: 0
          y: 0
          z: 0
        scale:
          x: 1
          y: 1
          z: 1
        is_static: true
      season_flag: None
  available_sounds:
    - chance: 100
      sound:
        sound_name: 'Lava.ogg'
        volume: 25
        loop: true
  enable_friendly_fire: Default
  enable_friendly_fire_autoban: Default
```

# Installation

## Requirements

- [Harmony](https://github.com/pardeike/Harmony) — required for the plugin (`0Harmony.dll`)
- LabAPI **1.1.7**
- [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) — required for Among Us
- [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI) — required for Among Us and AutoEvent.Vote
- [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio) — required for music playback
- [ProjectMER](https://github.com/Michal78900/ProjectMER) — required for map/schematic support

> - The AutoEvent release only contains the AutoEvent DLLs. Download every dependency from its own GitHub releases.
> - Every Dependency installation guide can be found in their GitHub ReadMes.

| Dependency                                                             | Required | Download                                                       |
|------------------------------------------------------------------------|----------|----------------------------------------------------------------|
| [Harmony](https://github.com/pardeike/Harmony)                         | Yes      | `0Harmony.dll`                                                 |
| [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) | No*      | `LabApiExtensions.dll` (*required for Among Us)                |
| [ProjectMER](https://github.com/Michal78900/ProjectMER)                | Yes      | `ProjectMER.dll`, map/schematic loading                        |
| [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI)             | No*      | `RadioMenuAPI.dll` (*required for Among Us and AutoEvent.Vote) |
| [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio)           | Yes      | `SecretLabNAudio.zip`, music playback                          |

---

## Step 1 — Download Files

Download `AutoEvent.dll` from the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest), and from
the dependencies' own releases:

- `0Harmony.dll` (skip if you already have Harmony installed)
- `RadioMenuAPI.dll`
- `LabApiExtensions.dll`
- `ProjectMER.dll`
- `SecretLabNAudio.zip`

---

## Step 2 — Install Files

**Plugin DLLs** — place `AutoEvent.dll`, `RadioMenuAPI.dll`, `LabApiExtensions.dll` and `ProjectMER.dll` in:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\plugins\global\
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/plugins/global/
```

**SecretLabNAudio** — extract `SecretLabNAudio.zip` and place `SecretLabNAudio.dll` in:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\plugins\global\
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/plugins/global/
```

**Harmony** — place `0Harmony.dll` in:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\dependencies\global\
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/dependencies/global/
```

**Music files** — nothing to do: the music is built into `AutoEvent.dll`. On start, every missing file is saved to:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\AutoEvent\Music\
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/AutoEvent/Music/
```

---

## Step 3 — Configure Paths

After first launch, verify the following paths in the AutoEvent config (`<port>` is the server port, e.g. `7777`):

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\AutoEvent\config.yml
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/<port>/AutoEvent/config.yml
```

The config needs full paths (`%AppData%` and `~` are not expanded). Windows:

```yaml
# Path to the schematics folder
schematics_directory_path: C:\Users\<user>\AppData\Roaming\SCP Secret Laboratory\LabAPI\configs\AutoEvent\Schematics

# Path to the music folder
music_directory_path: C:\Users\<user>\AppData\Roaming\SCP Secret Laboratory\LabAPI\configs\AutoEvent\Music
```

Linux (on Pterodactyl-style hosts the home folder is `/home/container`):

```yaml
# Path to the schematics folder
schematics_directory_path: /home/<user>/.config/SCP Secret Laboratory/LabAPI/configs/AutoEvent/Schematics

# Path to the music folder
music_directory_path: /home/<user>/.config/SCP Secret Laboratory/LabAPI/configs/AutoEvent/Music
```

Adjust these paths to match your server's file system if needed. These settings sometimes do not auto-generate
correctly — verify them manually before reporting issues.

---

## Step 4 — Set Permissions

Edit `permissions.yml` and add `ev.*` to the desired role:

```
Windows: %AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml
Linux:   ~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml
```

```yaml
owner:
  inheritance: []
  permissions:
    - ev.*
```

Available granular permissions:

```
ev.*           — All AutoEvent permissions
  ev.list      — View available events
  ev.run       — Start an event
  ev.stop      — Stop an event
  ev.reload    — Reload configs and translations
  ev.update    — Update schematics to latest versions
  ev.vote      — Start and end voting (requires AutoEvent.Vote plugin)
  ev.volume    — Change music volume
  ev.language  — Change language/translation
```

---

## Step 5 — Start the Server

Start your server and install the schematics using:

```
ev update
```

Verify AutoEvent loaded successfully by running:

```
ev list
```

If you see all mini-games listed, the installation is complete.

---

## Troubleshooting

See [Problem.md](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Problem.md) for common installation issues and
solutions.

# Installation

## Requirements

- Harmony — required for the plugin (included as `0Harmony.dll`)
- LabAPI **1.1.7**
- [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) — included in the release (required for Among Us)
- [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI) — included in the release (required for Among Us and AutoEvent.Vote)
- [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio) — included in the release as `SecretLabNAudio.zip`
- **Map Plugin**: [ProjectMER](https://github.com/Michal78900/ProjectMER) — required for map/schematic support

> - A verified working build of ProjectMER is available on
    the [AutoEvent Discord server](https://discord.gg/KmpA8cfaSA).
> - Every Dependency installation guide can be found in their GitHub ReadMes.

| Dependency                                                             | Required | Description                                        |
|------------------------------------------------------------------------|----------|----------------------------------------------------|
| Harmony                                                                | Yes      | Included in release as `0Harmony.dll`              |
| [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) | No*      | Included in release (*required for Among Us)       |
| [ProjectMER](https://github.com/Michal78900/ProjectMER)                | Yes      | Map/schematic loading                              |
| [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI)             | No*      | Included in release (*required for Among Us)       |
| [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio)           | Yes      | Included in release as `SecretLabNAudio.zip`       |

---

## Step 1 — Download Files

Download the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest). You need:

- `AutoEvent.dll`
- `0Harmony.dll` (skip if you already have Harmony installed)
- `RadioMenuAPI.dll`
- `LabApiExtensions.dll`
- `ProjectMER.dll`
- `SecretLabNAudio.zip`
- `Music.zip`

---

## Step 2 — Install Files

**Plugin DLLs** — place in `LabApi/plugins/global/`:

```
AutoEvent.dll
RadioMenuAPI.dll
LabApiExtensions.dll
ProjectMER.dll
```

**SecretLabNAudio** — extract `SecretLabNAudio.zip` and place `SecretLabNAudio.dll` in:

```
LabApi/plugins/global/
```

**Harmony** — place `0Harmony.dll` in:

```
LabApi/dependencies/global/
```

**Music files** — extract `Music.zip` to:

```
LabApi/configs/AutoEvent/Music/
```

**Optional: AutoEvent.Vote Plugin** — for the voting system (allows players to vote on mini-games):

Download `AutoEvent.Vote.dll` from the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest) and
place it in:

```
LabApi/plugins/global/AutoEvent.Vote.dll
```

> **Requirements for Vote plugin:**
> - RadioMenuAPI must be installed
> - Permission `ev.vote` must be configured in `permissions.yml`
> - See [Vote Documentation](Vote.md) for full setup and commands

---

## Step 3 — Configure Paths

After first launch, verify the following paths in the AutoEvent config (`LabApi/configs/global/AutoEvent/config.yml`):

```yaml
# Path to the schematics folder
schematics_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Schematics

# Path to the music folder
music_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Music
```

Adjust these paths to match your server's file system if needed. These settings sometimes do not auto-generate
correctly — verify them manually before reporting issues.

---

## Step 4 — Set Permissions

Edit `LabApi/configs/permissions.yml` and add `ev.*` to the desired role:

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

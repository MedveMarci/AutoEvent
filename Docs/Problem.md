# Troubleshooting

---

## 1. "You do not have permission to use this command"

You do not have the required permission node assigned to your role.

**Fix:** Add `ev.*` (or the specific `ev.<command>` node) to your role in `LabApi/configs/permissions.yml`.

See [Installation.md](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Installation.md) for full permission setup
instructions.

---

## 2. Only one mini-game shows in `ev list`

Only events that do not require a map are available. This means **ProjectMER is not installed or not detected**.

**Fix:** Install [ProjectMER](https://github.com/Michal78900/ProjectMER) for map/schematic support.

> A working build is available on my [Discord server](https://discord.gg/KmpA8cfaSA).

---

## 3. "ProjectMER was not detected"

ProjectMER is required for map-based events to load.

**Fix:** Install [ProjectMER](https://github.com/Michal78900/ProjectMER).

> A working build is available on my [Discord server](https://discord.gg/KmpA8cfaSA).

---

## 4. "You have installed an old version of ProjectMER"

Your installed version of ProjectMER is outdated.

**Fix:** Download the fixed version
from my [Discord server](https://discord.gg/KmpA8cfaSA).

---

## 5. "You need to download the map (X) to run this mini-game"

The required schematic is missing from your server.

**Fix:**

1. Download `Schematics.tar.gz` from
   the [latest AutoEvent release](https://github.com/MedveMarci/AutoEvent/releases/latest)
2. Extract the contents to `LabApi/configs/AutoEvent/Schematics/`

---

## 6. The event starts but something is wrong on the map

If the server loads correctly but the event behavior is broken, this is likely a plugin bug.

**Fix:** Open an issue on [GitHub](https://github.com/MedveMarci/AutoEvent/issues) with:

- A description of what happened
- The event command name
- Server logs from the time of the issue

---

## 7. "The mini-game (X) is not found"

The command name you used does not match any registered event.

**Fix:**

1. Run `ev list` in the Remote Admin console
2. Find the command name in square brackets — e.g. `[lava]`
3. Use that exact name: `ev run lava`

---

## 8. Paths not auto-generating correctly in config

The `schematics_directory_path` or `music_directory_path` values in your config are empty or incorrect.

**Fix:** Manually set them in `LabApi/configs/global/AutoEvent/config.yml`:

```yaml
schematics_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Schematics
music_directory_path: /home/container/.config/SCP Secret Laboratory/LabApi/configs/AutoEvent/Music
```

Adjust the paths to match your server's directory structure.

---

## 9. "Among Us requires RadioMenuAPI to run"

The Among Us event failed to start because RadioMenuAPI is not installed or not detected.

**Fix:** Install [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI):

1. Download the latest release of RadioMenuAPI
2. Place `RadioMenuAPI.dll` in `LabApi/plugins/global/`
3. Restart the server
4. Verify RadioMenuAPI loaded successfully in the server logs
5. Try starting Among Us again with `ev run amongus`

RadioMenuAPI is required for the Among Us voting and sabotage systems to function.

---

## 10. Among Us menus not appearing

The Among Us event started but voting/sabotage menus don't show.

**Fix:** Verify RadioMenuAPI is properly loaded:

1. Check that `RadioMenuAPI.dll` is in `LabApi/plugins/global/`
2. Check server logs for RadioMenuAPI load errors
3. Ensure no other plugins are conflicting with RadioMenuAPI
4. Restart the server and try again

If RadioMenuAPI was recently added, a full server restart is required.

---

## 11. "AutoEvent is already loaded! Remove the duplicate AutoEvent DLL"

Two AutoEvent DLLs are present simultaneously — one built with SecretLabNAudio and one with AudioPlayerAPI. The second
one detects this and refuses to load.

**Fix:** Keep only **one** `AutoEvent.dll` in `LabApi/plugins/global/`. Decide which audio backend you want and remove
the other DLL.
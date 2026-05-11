# AutoEvent

[![Version](https://img.shields.io/github/v/release/MedveMarci/AutoEvent?&label=Version&color=d500ff)](https://github.com/MedveMarci/AutoEvent/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.6-b84ee87)](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.6) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.6-blue?&color=e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Total Downloads](https://img.shields.io/github/downloads/MedveMarci/AutoEvent/total.svg?label=Total%20Downloads&color=ffbf00)]()

---

## Mini-Games plugin for SCP: Secret Laboratory

![Logo](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/MGMER.png)

AutoEvent is a mini-games plugin for SCP: Secret Laboratory, built on the **LabAPI** framework. It
includes **26 unique mini-games**.

---

## Guides

[![Mini-Games](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/MiniGames.md)
[![Installation](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message1.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Installation.md)
[![Commands](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message2.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Commands.md)
[![Language](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message3.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Language.md)
[![Problems](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message4.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Problem.md)
[![Plugin API](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message5.png)](https://github.com/MedveMarci/AutoEvent/blob/main/PLUGIN_API.md)
[![Configuration](https://github.com/MedveMarci/AutoEvent/blob/main/Photos/Message6.png)](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Configuration.md)

**Optional Modules:**

- [AutoEvent.Vote](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Vote.md) — voting system for choosing
  mini-games

<a href='https://discord.gg/KmpA8cfaSA'><img src='https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png' height="100"></a>

---

## Mini-Games (26 total)

| Command     | Name                  | Description                                            |
|-------------|-----------------------|--------------------------------------------------------|
| `airstrike` | Airstrike Party       | Survive as grenades rain down from above               |
| `amongus`   | Among Us              | Find the Impostor among the Crewmates on the Skeld map |
| `battle`    | Battle                | MTF vs Chaos Insurgency team battle                    |
| `chair`     | Musical Chairs        | Compete for free chairs to funny music                 |
| `cs`        | Counter-Strike        | Terrorists vs Counter-Terrorists on de_dust2           |
| `deathrun`  | Death Run             | Navigate a trap course without getting caught          |
| `dm`        | All Deathmatch        | Free-for-all deathmatch — most kills wins              |
| `dodge`     | Dodgeball             | Defeat the enemy team with balls                       |
| `fall`      | Fall Down             | Survive as the platform collapses beneath you          |
| `football`  | Football              | Score 3 goals to win                                   |
| `glass`     | Dead Jump             | Jump across fragile platforms to reach the end         |
| `gungame`   | Gun Game              | Earn new weapons with each kill — finish with a bat    |
| `hns`       | Hide And Seek         | Run and hide; pass the bat to survive                  |
| `jail`      | Simon's Prison        | CS 1.6 jail mode with events and challenges            |
| `knives`    | Knives of Death       | Team knife fight — last team standing wins             |
| `lava`      | The Floor is LAVA     | Climb the towers and shoot others as lava rises        |
| `light`     | Red Light Green Light | Reach the finish line — freeze when the light is red   |
| `line`      | Death Line            | Avoid the spinning platform to survive                 |
| `nukerun`   | Nuke Run              | Escape the facility before the warhead detonates       |
| `puzzle`    | Puzzle                | Stand on the correct colored platform before it falls  |
| `race`      | Race                  | Get to the end of the map before everyone else         |
| `spleef`    | Spleef                | Shoot platforms out from under other players           |
| `tag`       | Tag                   | Catch all players — pass the bat to avoid becoming it  |
| `tdm`       | Team Death-Match      | MTF vs Chaos on Shipment                               |
| `versus`    | Cock Fights           | Players duel in an arena — teams compete for points    |
| `zombie`    | Zombie Infection      | Infect all players before time runs out                |
| `zombie2`   | Zombie Survival       | Humans surviving against zombie players                |

---

## Dependencies

| Dependency                                                             | Required | Description                                        |
|------------------------------------------------------------------------|----------|----------------------------------------------------|
| Harmony                                                                | Yes      | Required for the plugin                            |
| [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) | No*      | LabAPI utility extensions (*required for Among Us) |
| [ProjectMER](https://github.com/Michal78900/ProjectMER)                | Yes      | Map/schematic loading                              |
| [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI)             | No*      | Radio menu system (*required for Among Us)         |

**Audio backend — pick one DLL variant:**

| DLL variant           | Audio plugin                                                    | Notes       |
|-----------------------|-----------------------------------------------------------------|-------------|
| `AutoEvent.dll`       | [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio)    | Recommended |
| `AutoEvent.APAPI.dll` | [AudioPlayerAPI](https://github.com/Killers0992/AudioPlayerApi) | Alternative |

> - A working version of ProjectMER can be found on the [Discord server](https://discord.gg/KmpA8cfaSA).
> - **Only install one AutoEvent DLL at a time.** If both variants are present, the second one will refuse to load.
> - Every Dependency installation guide can be found in their GitHub ReadMes.

---

## Quick Start

1. Download the [latest release](https://github.com/MedveMarci/AutoEvent/releases/latest):
    - `AutoEvent.dll` *(SecretLabNAudio)* **or** `AutoEvent.APAPI.dll` *(AudioPlayerAPI)* — pick one
    - `0Harmony.dll` (skip if Harmony is already installed)
    - `Music.zip`
2. Place `AutoEvent.dll` in `LabApi/plugins/global/`
3. Place `0Harmony.dll` in `LabApi/dependencies/global/`
4. Install the required dependencies listed above
5. Extract `Music.zip` to `LabApi/configs/AutoEvent/Music/`
6. Install [RadioMenuAPI](https://github.com/MedveMarci/RadioMenuAPI)
   and [LabApiExtensions](https://github.com/KadavasKingdom/LabApiExtensions) if you want to play Among Us.
7. Grant permissions in `LabApi/configs/permissions.yml`:
   ```yaml
   owner:
     permissions:
       - ev.*
   ```
8. Start the server.
9. Run `ev update` in the admin console to download the schematics.
10. Run `ev list` to verify all mini-games are loaded.

See the full [Installation Guide](https://github.com/MedveMarci/AutoEvent/blob/main/Docs/Installation.md) for details.

---

## Credits

- Original plugin by **RisottoMan**
- Maintained by **MedveMarci**
- Maps by **xleb.ik** and **PresidentFinny**
- Architecture assistance by **Redforce04**
- Bug fixes by **ART0022VI**
- Early development help by **Alexander666**
- Command code by **art15**
- Support by **Sakred_**
- Map plugin [ProjectMER](https://github.com/Michal78900/ProjectMER) by **Michal78900**
- Audio plugins [SecretLabNAudio](https://github.com/Axwabo/SecretLabNAudio) by **Axwabo**
  and [AudioPlayerAPI](https://github.com/Killers0992/AudioPlayerApi) by **Killers0992**

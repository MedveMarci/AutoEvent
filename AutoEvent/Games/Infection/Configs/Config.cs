using System.Collections.Generic;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Infection;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance("Zombie", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("sl_waterworld", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("ZombieRework", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("Zombie_PresidentFinny", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("Zombie_Xmas2024", new Vector3(0, 40f, 0f), season: SeasonFlags.Christmas));
        AvailableMaps.Add(
            new MapChance("ZombieRework_Xmas2024", new Vector3(0, 40f, 0f), season: SeasonFlags.Christmas));
        AvailableMaps.Add(new MapChance("Zombie_Halloween2024", new Vector3(0, 40f, 0f),
            season: SeasonFlags.Halloween));
        AvailableMaps.Add(new MapChance("ZombieRework_Halloween2024", new Vector3(0, 40f, 0f),
            season: SeasonFlags.Halloween));
    }

    public List<Loadout> PlayerLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    public List<Loadout> ZombieLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.Scp0492, 100 } },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    public List<string> ZombieScreams { get; set; } =
    [
        "human_death_01.ogg",
        "human_death_02.ogg"
    ];
}
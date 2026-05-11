using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Dodgeball;

public class Config : EventConfig
{
    [Description("A list of loadouts for team ClassD")]
    public readonly List<Loadout> ClassDLoadouts =
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

    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance("Dodgeball", new Vector3(0, 0, 30)));
        AvailableMaps.Add(new MapChance("Snowball", new Vector3(0, 0, 30), season: SeasonFlags.Christmas));
    }

    [Description("After how many seconds the round will end. [Default: 180]")]
    public int TotalTimeInSeconds { get; set; } = 180;

    [Description("A list of loadouts for team Scientist")]
    public List<Loadout> ScientistLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.Scientist, 100 } },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];
}
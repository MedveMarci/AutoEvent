using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Knives;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance("35hp_2", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("35hp_2_Xmas2024", new Vector3(0, 40f, 0f), season: SeasonFlags.Christmas));
        AvailableMaps.Add(new MapChance("35hp_2_Halloween2024", new Vector3(0, 40f, 0f),
            season: SeasonFlags.Halloween));
    }

    [Description("A list of loadouts that players on team 1 can get.")]
    public List<Loadout> Team1Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfCaptain, 100 } },

            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    [Description("A list of loadouts that players on team 2 can get.")]
    public List<Loadout> Team2Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ChaosRepressor, 100 } },

            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 3 }
            ]
        }
    ];
}
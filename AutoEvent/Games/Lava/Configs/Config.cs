using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Lava;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance(50, new MapInfo("Lava", new Vector3(0f, 40f, 0f))));
        AvailableMaps.Add(new MapChance(50, new MapInfo("Lava_Xmas2024", new Vector3(0f, 40f, 0f)),
            SeasonFlags.Christmas));
    }

    [Description("A list of available loadouts that players can get.")]
    public List<Loadout> Loadouts { get; set; } =
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
}
using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class Config : EventConfig
{
    public Config()
    {
        if (AvailableMaps is null) AvailableMaps = [];

        if (AvailableMaps.Count < 1)
        {
            AvailableMaps.Add(new MapChance(50, new MapInfo("TempleMap", new Vector3(0f, 30f, 30f))));
            AvailableMaps.Add(new MapChance(50, new MapInfo("TempleMap_Xmas2025", new Vector3(0f, 30f, 30f)),
                SeasonFlags.Christmas));
        }
    }

    [Description("How long the round should last in minutes.")]
    public int RoundDurationInSeconds { get; set; } = 300;

    [Description("How many seconds after the start of the game can be given a second life? Disable -> -1")]
    public int SecondLifeInSeconds { get; set; } = 15;

    [Description("Loadouts of run-guys")]
    public List<Loadout> PlayerLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },

            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }],

            Chance = 100,
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];

    [Description("Loadouts of death-guys")]
    public List<Loadout> DeathLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.Scientist, 100 } },


            Effects =
            [
                new EffectData { Type = nameof(MovementBoost), Intensity = 50, Duration = 0 },
                new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }
            ],

            Chance = 100
        }
    ];

    [Description("Weapon loadouts for finish")]
    public List<Loadout> WeaponLoadouts { get; set; } =
    [
        new()
        {
            Items = [ItemType.GunE11SR, ItemType.Jailbird],
            Effects = [new EffectData { Type = nameof(MovementBoost), Intensity = 50, Duration = 0 }],

            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];

    [Description("Poison loadouts")]
    public List<Loadout> PoisonLoadouts { get; set; } =
    [
        new()
        {
            Effects = [new EffectData { Type = nameof(CardiacArrest), Intensity = 1, Duration = 15 }]
        }
    ];
}
using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using UnityEngine;

namespace AutoEvent.Games.Battle.Configs;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance(50, new MapInfo("Battle", new Vector3(0f, 40f, 0f))));
        AvailableMaps.Add(new MapChance(50, new MapInfo("Battle_Xmas2025", new Vector3(0f, 40f, 0f)),
            SeasonFlags.Christmas));
    }

    [Description("A List of Loadouts to use.")]
    public List<Loadout> Loadouts { get; set; } =
    [
        new()
        {
            Health = 100,
            Chance = 33,
            Items =
            [
                ItemType.GunE11SR, ItemType.Medkit, ItemType.Medkit,
                ItemType.ArmorCombat, ItemType.SCP1853, ItemType.Adrenaline
            ],

            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        },

        new()
        {
            Health = 115,
            Chance = 33,
            Items =
            [
                ItemType.GunShotgun, ItemType.Medkit, ItemType.Medkit,
                ItemType.Medkit, ItemType.Medkit, ItemType.Medkit,
                ItemType.ArmorCombat, ItemType.SCP500
            ],
            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        },

        new()
        {
            Health = 200,
            Chance = 33,
            Items =
            [
                ItemType.GunLogicer, ItemType.ArmorHeavy, ItemType.SCP500,
                ItemType.SCP500, ItemType.SCP1853, ItemType.Medkit
            ],
            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];
}
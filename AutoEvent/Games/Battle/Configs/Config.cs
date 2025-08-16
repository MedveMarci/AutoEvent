using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using UnityEngine;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
#else
using CustomPlayerEffects;
#endif

namespace AutoEvent.Games.Battle;

public class Config : EventConfig
{
    public Config()
    {
        if (AvailableMaps is null) AvailableMaps = new List<MapChance>();

        if (AvailableMaps.Count < 1)
        {
            AvailableMaps.Add(new MapChance(50, new MapInfo("Battle", new Vector3(0f, 40f, 0f))));
            AvailableMaps.Add(new MapChance(50, new MapInfo("Battle_Xmas2025", new Vector3(0f, 40f, 0f)),
                SeasonFlags.Christmas));
        }
    }

    [Description("A List of Loadouts to use.")]
    public List<Loadout> Loadouts { get; set; } = new()
    {
        new Loadout
        {
            Health = 100,
            Chance = 33,
            Items = new List<ItemType>
            {
                ItemType.GunE11SR, ItemType.Medkit, ItemType.Medkit,
                ItemType.ArmorCombat, ItemType.SCP1853, ItemType.Adrenaline
            },
#if EXILED
#if EXILED
            Effects = new List<Effect> { new(EffectType.FogControl, 0) },
#else
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
#endif
#else
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
#endif
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        },
        new Loadout
        {
            Health = 115,
            Chance = 33,
            Items = new List<ItemType>
            {
                ItemType.GunShotgun, ItemType.Medkit, ItemType.Medkit,
                ItemType.Medkit, ItemType.Medkit, ItemType.Medkit,
                ItemType.ArmorCombat, ItemType.SCP500
            },
#if EXILED
            Effects = new List<Effect> { new(EffectType.FogControl, 0) },
#else
            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 0, Duration = 0 }],
#endif
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        },
        new Loadout
        {
            Health = 200,
            Chance = 33,
            Items = new List<ItemType>
            {
                ItemType.GunLogicer, ItemType.ArmorHeavy, ItemType.SCP500,
                ItemType.SCP500, ItemType.SCP1853, ItemType.Medkit
            },
#if EXILED
            Effects = new List<Effect> { new(EffectType.FogControl, 0) },
#else
            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 0, Duration = 0 }],
#endif
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    };
}
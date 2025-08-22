using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.GunGame;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance(50, new MapInfo("Shipment", new Vector3(0, 40f, 0f))));
        AvailableMaps.Add(new MapChance(50, new MapInfo("Shipment_Xmas2025", new Vector3(0, 40f, 0f)),
            SeasonFlags.Christmas));
        AvailableMaps.Add(new MapChance(50, new MapInfo("Shipment_Halloween2024", new Vector3(0, 40f, 0f)),
            SeasonFlags.Halloween));
    }

    [Description("A list of guns a player can get.")]
    public List<GunRole> Guns { get; set; } =
    [
        new(ItemType.GunCOM15, 0),
        new(ItemType.GunCOM18, 2),
        new(ItemType.GunRevolver, 4),
        new(ItemType.GunCom45, 6),
        new(ItemType.GunFSP9, 8),
        new(ItemType.GunCrossvec, 10),
        new(ItemType.GunAK, 12),
        new(ItemType.Jailbird, 14),
        new(ItemType.GunE11SR, 16),
        new(ItemType.GunRevolver, 18),
        new(ItemType.GunA7, 20),
        new(ItemType.ParticleDisruptor, 22),
        new(ItemType.GunAK, 24),
        new(ItemType.GunE11SR, 26),
        new(ItemType.GunLogicer, 28),
        new(ItemType.GunFRMG0, 30),
        new(ItemType.Jailbird, 32),
        new(ItemType.None, 34)
    ];

    [Description("The loadouts a player can get.")]
    public List<Loadout> Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int>
            {
                { RoleTypeId.ClassD, 100 },
                { RoleTypeId.Scientist, 100 },
                { RoleTypeId.NtfSergeant, 100 },
                { RoleTypeId.ChaosRifleman, 100 },
                { RoleTypeId.FacilityGuard, 100 }
            },
            InfiniteAmmo = AmmoMode.InfiniteAmmo,
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];
}

public class GunRole
{
    public GunRole()
    {
    }

    public GunRole(ItemType item, int killsRequired)
    {
        Item = item;
        KillsRequired = killsRequired;
    }

    [Description("The weapon that the player will recieve once they get to this role.")]
    public ItemType Item { get; set; } = ItemType.GunCOM15;

    [Description("Total kills needed to get this gun. [Default: 1]")]
    public int KillsRequired { get; set; } = 1;
}
using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;

namespace AutoEvent.Games.Jail;

public class Config : EventConfig
{
    [Description("How many lives each prisoner gets.")]
    public int PrisonerLives { get; set; } = 3;

    [Description("How many players will spawn as the jailors.")]
    public RoleCount JailorRoleCount { get; set; } = new(1, 4, 15f);

    [Description("A list of loadouts for the jailors.")]
    public List<Loadout> JailorLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfCaptain, 100 } },
            Items =
            [
                ItemType.GunE11SR,
                ItemType.GunCOM18
            ],
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    [Description("A list of loadouts for the prisoners.")]
    public List<Loadout> PrisonerLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int>
            {
                { RoleTypeId.ClassD, 100 }
            },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];

    [Description("What loadouts each locker can give.")]
    public List<Loadout> WeaponLockerLoadouts { get; set; } =
    [
        new()
        {
            InfiniteAmmo = AmmoMode.InfiniteAmmo,
            Items =
            [
                ItemType.GunE11SR,
                ItemType.GunCOM15
            ]
        },

        new()
        {
            InfiniteAmmo = AmmoMode.InfiniteAmmo,
            Items =
            [
                ItemType.GunCrossvec,
                ItemType.GunRevolver
            ]
        }
    ];

    public List<Loadout> MedicalLoadouts { get; set; } =
    [
        new()
        {
            Health = 100
        }
    ];

    public List<Loadout> AdrenalineLoadouts { get; set; } =
    [
        new()
        {
            ArtificialHealth = new ArtificialHealth
            {
                InitialAmount = 100f,
                MaxAmount = 100f,
                RegenerationAmount = 0,
                AbsorptionPercent = 70,
                Permanent = false,
                Duration = 0
            }
        }
    ];
}
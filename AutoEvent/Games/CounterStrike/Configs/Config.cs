using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;

namespace AutoEvent.Games.CounterStrike;

public class Config : EventConfig
{
    [Description("After how many seconds the round will end. [Default: 105]")]
    public int TotalTimeInSeconds { get; set; } = 105;

    [Description("A list of loadouts for team NTF")]
    public List<Loadout> NtfLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfSpecialist, 100 } },
            Items =
                [ItemType.GunE11SR, ItemType.GrenadeHE, ItemType.GrenadeFlash, ItemType.Radio, ItemType.ArmorCombat],
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];

    [Description("A list of loadouts for team Chaos Insurgency")]
    public List<Loadout> ChaosLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ChaosRifleman, 100 } },
            Items = [ItemType.GunAK, ItemType.GrenadeHE, ItemType.GrenadeFlash, ItemType.Radio, ItemType.ArmorCombat],
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
            InfiniteAmmo = AmmoMode.InfiniteAmmo
        }
    ];
}
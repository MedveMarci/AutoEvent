using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;

namespace AutoEvent.Games.AllDeathmatch.Configs;

public class Config : EventConfig
{
    [Description("How many minutes should we wait for the end of the round.")]
    public int TimeMinutesRound { get; set; } = 10;

    [Description("A list of loadouts for team NTF")]
    public List<Loadout> NtfLoadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfSpecialist, 100 } },
            Items = [ItemType.ArmorCombat],
            InfiniteAmmo = AmmoMode.InfiniteAmmo,
            Effects =
            [
                new EffectData { Type = nameof(MovementBoost), Duration = 10, Intensity = 0 },
                new EffectData { Type = nameof(Scp1853), Duration = 1, Intensity = 0 },
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    [Description("The weapons a player can get once the round starts.")]
    public List<ItemType> AvailableWeapons { get; set; } =
    [
        ItemType.GunAK,
        ItemType.GunCrossvec,
        ItemType.GunFSP9,
        ItemType.GunE11SR
    ];
}
using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using PlayerRoles;
#if EXILED
using Exiled.API.Features;
using Exiled.API.Enums;
#else
using CustomPlayerEffects;
#endif

namespace AutoEvent.Games.AllDeathmatch;

public class Config : EventConfig
{
    [Description("How many minutes should we wait for the end of the round.")]
    public int TimeMinutesRound { get; set; } = 10;

    [Description("A list of loadouts for team NTF")]
    public List<Loadout> NTFLoadouts { get; set; } = new()
    {
        new Loadout
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfSpecialist, 100 } },
            Items = new List<ItemType> { ItemType.ArmorCombat },
            InfiniteAmmo = AmmoMode.InfiniteAmmo,
#if EXILED
            Effects = new List<Effect>
            {
                new(EffectType.MovementBoost, 10, 0),
                new(EffectType.Scp1853, 1, 0),
                new(EffectType.FogControl, 0)
            }
#else
            Effects =
            [
                new EffectData { Type = nameof(MovementBoost), Duration = 10, Intensity = 0 },
                new EffectData { Type = nameof(Scp1853), Duration = 1, Intensity = 0 },
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 0 }
            ]
#endif
        }
    };

    [Description("The weapons a player can get once the round starts.")]
    public List<ItemType> AvailableWeapons { get; set; } = new()
    {
        ItemType.GunAK,
        ItemType.GunCrossvec,
        ItemType.GunFSP9,
        ItemType.GunE11SR
    };
}
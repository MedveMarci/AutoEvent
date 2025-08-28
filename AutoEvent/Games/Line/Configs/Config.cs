using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;

namespace AutoEvent.Games.Line;

public class Config : EventConfig
{
    [Description("A list of loadouts players can get.")]
    public List<Loadout> Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int>
            {
                { RoleTypeId.Scientist, 100 }
            },

            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    [Description("A list of loadouts players can get.")]
    public List<Loadout> FailureLoadouts { get; set; } =
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
            ]
        }
    ];
}
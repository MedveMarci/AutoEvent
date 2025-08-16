using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using PlayerRoles;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
#else
using CustomPlayerEffects;
#endif

namespace AutoEvent.Games.Race;

public class Config : EventConfig
{
    [Description("How long the event should last in seconds. [Default: 60]")]
    public int EventDurationInSeconds { get; set; } = 60;

    [Description("A list of loadouts players can get.")]
    public List<Loadout> Loadouts { get; set; } = new()
    {
        new Loadout
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },
#if EXILED
            Effects = new List<Effect> { new(EffectType.FogControl, 0) },
#else
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ],
#endif
        }
    };
}
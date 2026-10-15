using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Versus;

public class Config : EventConfig
{
    [Description("Can be used to disable the jailbird charging attack.")]
    public bool JailbirdCanCharge { get; set; } = false;

    [Description("How many rounds a player can play in a row. After that they are taken out of the arena and have to sit out one round. Set to 0 for infinite rounds.")]
    public int JailbirdLifespan { get; set; } = 0;

    [Description("How long to wait before forcefully selecting a random player. Set to -1 to disable.")]
    public int AutoSelectDelayInSeconds { get; set; } = 10;

    [Description("Loadouts for team 1.")]
    public List<Loadout> Team1Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.Scientist, 100 } },

            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }]
        }
    ];

    [Description("Loadouts for team 2.")]
    public List<Loadout> Team2Loadouts { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },

            Effects = [new EffectData { Type = nameof(FogControl), Intensity = 1, Duration = 0 }]
        }
    ];

    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance("35Hp", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("35Hp_PresidentFinny", new Vector3(0, 40f, 0f)));
        AvailableMaps.Add(new MapChance("35Hp_Xmas2024", new Vector3(0, 40f, 0f), season: SeasonFlags.Christmas));
        AvailableMaps.Add(new MapChance("35Hp_Halloween2024", new Vector3(0, 40f, 0f), season: SeasonFlags.Halloween));
    }
}
﻿using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Football;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance(50, new MapInfo("Football", new Vector3(0f, 40f, 0f))));
        AvailableMaps.Add(new MapChance(50, new MapInfo("Football_Xmas2025", new Vector3(0f, 40f, 0f)),
            SeasonFlags.Christmas));
    }

    [Description("How many points a team needs to get to win. [Default: 3]")]
    public int PointsToWin { get; set; } = 3;

    [Description("How long the match should take in seconds. [Default: 180]")]
    public int MatchDurationInSeconds { get; set; } = 180;

    [Description("A List of Loadouts to use.")]
    public List<Loadout> BlueTeamLoadout { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.NtfCaptain, 100 } },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];

    [Description("A List of Loadouts to use.")]
    public List<Loadout> OrangeTeamLoadout { get; set; } =
    [
        new()
        {
            Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },
            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 }
            ]
        }
    ];
}
﻿using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Light;

public class Config : EventConfig
{
    public Config()
    {
        AvailableMaps ??= [];

        if (AvailableMaps.Count >= 1) return;
        AvailableMaps.Add(new MapChance(50, new MapInfo("RedLight", new Vector3(0, 40f, 0f))));
        AvailableMaps.Add(new MapChance(50, new MapInfo("RedLight_Xmas2024", new Vector3(0, 40f, 0f)),
            SeasonFlags.Christmas));
    }

    [Description("After how many seconds the round will end. [Default: 70]")]
    public int TotalTimeInSeconds { get; set; } = 70;

    [Description("The players will push each other for fun. Default: true.")]
    public bool IsEnablePush { get; set; } = true;

    [Description("How much time should I give the player in seconds to cool down to use the push?")]
    public float PushPlayerCooldown { get; set; } = 5;

    [Description("A list of loadouts for team ClassD")]
    public List<Loadout> PlayerLoadout { get; set; } =
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
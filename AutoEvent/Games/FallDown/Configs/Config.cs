using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Season.Enum;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.FallDown;

public class Config : EventConfig
{
    public Config()
    {
        if (AvailableMaps is null) AvailableMaps = [];

        if (AvailableMaps.Count < 1)
        {
            AvailableMaps.Add(new MapChance(50, new MapInfo("FallDown", new Vector3(0f, 40f, 0f))));
            AvailableMaps.Add(new MapChance(50, new MapInfo("FallDown_Xmas2024", new Vector3(0f, 40f, 0f)),
                SeasonFlags.Christmas));
        }
    }

    [Description("The delay between the selection of platforms that fall from 2 to 0.1. [Default: 1 - 0.3]")]
    public DifficultyItem DelayInSeconds { get; set; } = new(1, 0.3f);

    [Description("The delay between the color warning, and the platform falling from 3 to 0. [Default 0.7 - 0]")]
    public DifficultyItem WarningDelayInSeconds { get; set; } = new(0.7f, 0f);

    [Description("Should platforms have a color warning for when they are about to fall.")]
    public bool PlatformsHaveColorWarning { get; set; } = false;

    [Description("A List of Loadouts to use.")]
    public List<Loadout> Loadouts { get; set; } =
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
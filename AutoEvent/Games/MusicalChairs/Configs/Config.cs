using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.MusicalChairs;

public class Config : EventConfig
{
    public Config()
    {
        if (AvailableMaps is null) AvailableMaps = [];

        if (AvailableMaps.Count < 1)
        {
            AvailableMaps.Add(new MapChance("MusicalChairs", new Vector3(0f, 40f, 0f)));
            AvailableMaps.Add(new MapChance("MusicalChairs_Xmas2024", new Vector3(0f, 40f, 0f),
                season: SeasonFlags.Christmas));
        }
    }

    [Description("A loadout for players")]
    public List<Loadout> PlayerLoadout { get; set; } =
    [
        new()
        {
            Health = 100,
            Roles = new Dictionary<RoleTypeId, int>
            {
                { RoleTypeId.ClassD, 50 },
                { RoleTypeId.Scientist, 50 }
            },

            Effects =
            [
                new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 1 },
                new EffectData { Type = nameof(HeavyFooted), Duration = 0, Intensity = 255 }
            ],

            Stamina = 0
        }
    ];
}
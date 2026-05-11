using System.Collections.Generic;
using System.ComponentModel;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using PlayerRoles;

namespace AutoEvent.Games.AmongUs.Configs;

public class Config : EventConfig
{
    public Loadout Loadout { get; set; } = new()
    {
        Roles = new Dictionary<RoleTypeId, int> { { RoleTypeId.ClassD, 100 } },
        Items = [],
        Effects =
        [
            new EffectData { Type = nameof(FogControl), Duration = 0, Intensity = 2 },
            new EffectData { Type = nameof(Fade), Duration = 0, Intensity = 255 }
        ],
        InfiniteAmmo = AmmoMode.NoReloadInfiniteAmmo
    };

    [Description("The amount of Impostors that can spawn.")]
    public RoleCount Impostors { get; set; } = new() { MinimumPlayers = 1, MaximumPlayers = 3, Percentage = 10 };

    public float KillCooldown { get; set; } = 45f;
    public float SabotageCooldown { get; set; } = 45f;

    public int EmergencyMeetings { get; set; } = 1;
    public int EmergencyCooldown { get; set; } = 10;
    public float VotingTime { get; set; } = 45f;
    public float DiscussionTime { get; set; } = 45f;
    public bool AnonymousVotes { get; set; } = false;
    public bool ConfirmEjects { get; set; } = true;
    public bool VisualTasks { get; set; } = true;

    public int CommonTasks { get; set; } = 1;
    public int LongTasks { get; set; } = 1;
    public int ShortTasks { get; set; } = 2;
}
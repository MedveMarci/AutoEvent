using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Games.Battle.Configs;
using AutoEvent.Interfaces;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Extensions = AutoEvent.API.Extensions;

namespace AutoEvent.Games.Battle;

public class Plugin : Event<Configs.Config, Translation>, IEventMap, IEventSound
{
    private List<GameObject> _workstations;
    public override string Name { get; set; } = "Battle";
    public override string Description { get; set; } = "MTF fight against CI in an arena";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "battle";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreDroppingItem;
    protected override float FrameDelayInSeconds { get; set; } = 1f;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Battle",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "MetalGearSolid.ogg",
        Volume = 10,
        Loop = false
    };

    protected override void OnStart()
    {
        List<GameObject> ntfSpawns = [];
        List<GameObject> chaosSpawns = [];
        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint": ntfSpawns.Add(gameObject); break;
                case "Spawnpoint1": chaosSpawns.Add(gameObject); break;
            }

        var count = 0;
        foreach (var player in Player.ReadyList)
        {
            if (count % 2 == 0)
            {
                player.SetRole(RoleTypeId.NtfSergeant, flags: RoleSpawnFlags.None);
                player.Position = ntfSpawns.RandomItem().transform.position;
            }
            else
            {
                player.SetRole(RoleTypeId.ChaosConscript, flags: RoleSpawnFlags.None);
                player.Position = chaosSpawns.RandomItem().transform.position;
            }

            count++;

            player.GiveLoadout(Config.Loadouts, LoadoutFlags.IgnoreRole);
            player.CurrentItem = player.Items.FirstOrDefault(r => nameof(r.Type).Contains("Gun"));
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 20; time > 0; time--)
        {
            Extensions.ServerBroadcast(Translation.TimeLeft.Replace("{time}", $"{time}"), 5);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        // Once the countdown has ended, we need to destroy the walls, and add workstations.
        _workstations = [];
        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Wall":
                {
                    NetworkServer.Destroy(gameObject);
                }
                    break;
                case "Workstation":
                {
                    _workstations.Add(gameObject);
                }
                    break;
            }
    }

    protected override bool IsRoundDone()
    {
        // Round finishes when either team has no more players.
        return Player.ReadyList.Count(x => x.RoleBase.Team == Team.FoundationForces) == 0 ||
               Player.ReadyList.Count(x => x.RoleBase.Team == Team.ChaosInsurgency) == 0;
    }

    protected override void ProcessFrame()
    {
        // While the round isn't done, this will be called once a second. You can make the call duration faster / slower by changing FrameDelayInSeconds.
        // While the round is still going, broadcast the current round stats.
        var text = Translation.Counter;
        text = text.Replace("{FoundationForces}",
            $"{Player.ReadyList.Count(r => r.RoleBase.Team == Team.FoundationForces)}");
        text = text.Replace("{ChaosForces}", $"{Player.ReadyList.Count(r => r.RoleBase.Team == Team.ChaosInsurgency)}");
        text = text.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}");

        Extensions.ServerBroadcast(text, 1);
    }

    protected override void OnFinished()
    {
        // Once the round is finished, broadcast the winning team (either mtf or chaos in this case.)
        // If the round is stopped, this won't be called. Instead, use OnStop to broadcast either winners, or that nobody wins because the round was stopped.
        if (Player.ReadyList.Count(r => r.RoleBase.Team == Team.FoundationForces) == 0)
            Extensions.ServerBroadcast(
                Translation.CiWin.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"),
                3);
        else // if (Player.GetPlayers().Count(r => r.Team == Team.ChaosInsurgency) == 0)
            Extensions.ServerBroadcast(
                Translation.MtfWin.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"),
                10);
    }

    protected override void OnCleanup()
    {
        // 10 seconds after finishing the round or once the round is stopped, this will be called.
        // If 10 seconds is too long, you can change PostRoundDelay to make it faster or shorter.
        // We can clean up extra workstations that we spawned in. 
        // The map will be cleaned up for us, as well as items, ragdolls, and sound.
        foreach (var bench in _workstations)
            Object.Destroy(bench);
    }
}
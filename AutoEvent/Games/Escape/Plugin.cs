using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using UnityEngine;
using ElevatorDoor = Interactables.Interobjects.ElevatorDoor;

namespace AutoEvent.Games.Escape;

public class Plugin : Event<Config, Translation>, IEventSound
{
    public override string Name { get; set; } = "Atomic Escape";
    public override string Description { get; set; } = "Escape from the facility behind SCP-173 at supersonic speed!";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "escape";
    private EventHandler EventHandler { get; set; }

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Escape.ogg",
        Volume = 25,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        EventHandler = new EventHandler(this);
        PlayerEvents.Joined += EventHandler.OnJoined;
        ServerEvents.CassieAnnouncing += EventHandler.OnAnnoucingScpTermination;
        Scp173Events.CreatingTantrum += EventHandler.OnPlacingTantrum;
        Scp173Events.BreakneckSpeedChanging += EventHandler.OnUsingBreakneckSpeeds;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Joined -= EventHandler.OnJoined;
        ServerEvents.CassieAnnouncing -= EventHandler.OnAnnoucingScpTermination;
        Scp173Events.CreatingTantrum -= EventHandler.OnPlacingTantrum;
        Scp173Events.BreakneckSpeedChanging -= EventHandler.OnUsingBreakneckSpeeds;
        EventHandler = null;
    }

    protected override bool IsRoundDone()
    {
        return !(EventTime.TotalSeconds <= Config.EscapeDurationTime &&
                 Player.ReadyList.Count(r => r.IsAlive) > 0);
    }

    protected override void OnStart()
    {
        var startPos = new GameObject
        {
            transform =
            {
                parent = Room.List.First(r => r.Name == RoomName.Lcz173).Transform,
                localPosition = new Vector3(16.5f, 13f, 8f)
            }
        };
        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Scp173Loadout);
            player.Position = startPos.transform.position;
            player.EnableEffect<Ensnared>(1, 10);
            player.EnableEffect<MovementBoost>(50);
        }

        AlphaWarheadController.Singleton.CurScenario.AdditionalTime = Config.EscapeResumeTime;
        Warhead.Start();
        Warhead.IsLocked = true;
    }

    protected override void ProcessFrame()
    {
        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{time}",
                (Config.EscapeDurationTime - EventTime.TotalSeconds).ToString("00")), 1);
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast(
                Translation.BeforeStart.Replace("{name}", Name).Replace("{time}", time.ToString()), 1);
            yield return Timing.WaitForSeconds(1f);
        }

        foreach (var door in DoorVariant.AllDoors.Where(door => door is not ElevatorDoor))
            door.NetworkTargetState = true;
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.ReadyList)
        {
            player.EnableEffect<Flashed>(1, 1);

            if (player.Zone != FacilityZone.Surface) player.Kill("You failed to escape in time!");
        }

        var playerAlive = Player.ReadyList.Count(x => x.IsAlive).ToString();
        Extensions.ServerBroadcast(Translation.End.Replace("{name}", Name).Replace("{players}", playerAlive), 10);
    }

    protected override void OnCleanup()
    {
        Warhead.IsLocked = false;
        Warhead.Stop();
    }
}
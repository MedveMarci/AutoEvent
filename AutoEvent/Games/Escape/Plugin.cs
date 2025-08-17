#if EXILED
using Exiled.API.Features;
using EffectType = Exiled.API.Enums.EffectType;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
#endif
using System.Collections.Generic;
using System.Linq;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
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
    private EventHandler _eventHandler { get; set; }

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Escape.ogg",
        Volume = 25,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Exiled.Events.Handlers.Player.Joined += _eventHandler.OnJoined;
        Exiled.Events.Handlers.Map.AnnouncingScpTermination += _eventHandler.OnAnnoucingScpTermination;
        Exiled.Events.Handlers.Scp173.PlacingTantrum += _eventHandler.OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds += _eventHandler.OnUsingBreakneckSpeeds;
#else
        PlayerEvents.Joined += _eventHandler.OnJoined;
        ServerEvents.CassieAnnouncing += _eventHandler.OnAnnoucingScpTermination;
        Scp173Events.CreatingTantrum += _eventHandler.OnPlacingTantrum;
        Scp173Events.BreakneckSpeedChanging += _eventHandler.OnUsingBreakneckSpeeds;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Exiled.Events.Handlers.Player.Joined -= _eventHandler.OnJoined;
        Exiled.Events.Handlers.Map.AnnouncingScpTermination -= _eventHandler.OnAnnoucingScpTermination;
        Exiled.Events.Handlers.Scp173.PlacingTantrum -= _eventHandler.OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds -= _eventHandler.OnUsingBreakneckSpeeds;
#else
        PlayerEvents.Joined -= _eventHandler.OnJoined;
        ServerEvents.CassieAnnouncing -= _eventHandler.OnAnnoucingScpTermination;
        Scp173Events.CreatingTantrum -= _eventHandler.OnPlacingTantrum;
        Scp173Events.BreakneckSpeedChanging -= _eventHandler.OnUsingBreakneckSpeeds;
#endif
        _eventHandler = null;
    }

    protected override bool IsRoundDone()
    {
        return !(EventTime.TotalSeconds <= Config.EscapeDurationTime &&
                 Player.List.Count(r => r.IsAlive) > 0);
    }

    protected override void OnStart()
    {
        var _startPos = new GameObject();
#if EXILED
        _startPos.transform.parent = Room.List.First(r => r.Identifier.Name == RoomName.Lcz173).Transform;
#else
        _startPos.transform.parent = Room.List.First(r => r.Name == RoomName.Lcz173).Transform;
#endif
        _startPos.transform.localPosition = new Vector3(16.5f, 13f, 8f);
#if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            player.GiveLoadout(Config.Scp173Loadout);
            player.Position = _startPos.transform.position;
#if EXILED
            player.EnableEffect(EffectType.Ensnared, 1, 10);
            player.EnableEffect(EffectType.MovementBoost, 50);
#else
            player.EnableEffect<Ensnared>(1, 10);
            player.EnableEffect<MovementBoost>(50);
#endif
        }

        AlphaWarheadController.Singleton.CurScenario.AdditionalTime = Config.EscapeResumeTime;
        Warhead.Start();
        Warhead.IsLocked = true;
    }

    protected override void ProcessFrame()
    {
        Extensions.Broadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{time}",
                (Config.EscapeDurationTime - EventTime.TotalSeconds).ToString("00")), 1);
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.Broadcast(
                Translation.BeforeStart.Replace("{name}", Name).Replace("{time}", time.ToString()), 1);
            yield return Timing.WaitForSeconds(1f);
        }

        foreach (var door in DoorVariant.AllDoors)
            if (door is not ElevatorDoor)
                door.NetworkTargetState = true;

        yield break;
    }

    protected override void OnFinished()
    {
        foreach (var player in Player.List)
        {
            player.EnableEffect<Flashed>(1, 1);

            if (player.Position.y < 980f) player.Kill("You didn't have time");
        }

        var playeAlive = Player.List.Count(x => x.IsAlive).ToString();
        Extensions.Broadcast(Translation.End.Replace("{name}", Name).Replace("{players}", playeAlive), 10);
    }

    protected override void OnCleanup()
    {
        Warhead.IsLocked = false;
        Warhead.Stop();
    }
}
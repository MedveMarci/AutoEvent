using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using PlayerRoles;
using UnityEngine;
using ElevatorDoor = Interactables.Interobjects.ElevatorDoor;

namespace AutoEvent.Games.NukeRun;

public class Plugin : Event<Config, Translation>, IEventSound
{
    public override string Name { get; set; } = "Nuke Run";

    public override string Description { get; set; } = "Escape from the facility before the Nuke explodes!";

    public override string Author { get; set; } = "RisottoMan && MedveMarci";

    public override string CommandName { get; set; } = "nukerun";

    private EventHandler EventHandler { get; set; }

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Escape.ogg",
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
        return !(EventTime.TotalSeconds <= Config.EscapeDurationTime && Player.ReadyList.Count(r => r.IsAlive) > 0);
    }

    protected override void OnStart()
    {
        GameObject startPos = new()
        {
            transform =
            {
                parent = Room.List.First(r => r.Name == RoomName.Lcz173).Transform,
                localPosition = new Vector3(16.5f, 13f, 8f)
            }
        };
        foreach (Player player in Player.ReadyList)
        {
            player.SetRole(Config.SpawnAsScp173 ? RoleTypeId.Scp173 : RoleTypeId.ClassD);
            if (!Config.SpawnAsScp173) continue;
            player.Position = startPos.transform.position;
            player.EnableEffect<Ensnared>(1, 11);
        }

        Object.Destroy(startPos);

        Warhead.Scenario = Warhead.StartScenarios.First();
        Warhead.DetonationTime = Config.EscapeDurationTime;
        Warhead.Start();
        Warhead.IsLocked = true;
        Warhead.OpenBlastDoors();
        foreach (Door door in Door.List.Where(door => door.DoorName is DoorName.EzGateA or DoorName.EzGateB or DoorName.Lcz173Gate))
            door.IsOpened = true;
    }

    protected override void ProcessFrame()
    {
        Extensions.ServerBroadcast(Translation.Cycle.Replace("{name}", Name).Replace("{time}", (Config.EscapeDurationTime - EventTime.TotalSeconds).ToString("00")), 1);
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (int time = 11; time > 0; time--)
        {
            Extensions.ServerBroadcast(Translation.BeforeStart.Replace("{name}", Name).Replace("{time}", time.ToString()), 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void OnFinished()
    {
        string playerAlive = Player.ReadyList.Count(x => x.IsAlive).ToString();
        Extensions.ServerBroadcast(Translation.End.Replace("{name}", Name).Replace("{players}", playerAlive), 10);
    }

    protected override void OnCleanup()
    {
        Warhead.IsLocked = false;
        Warhead.Start();
        Warhead.Stop();
        foreach (DoorVariant door in DoorVariant.AllDoors.Where(door => door is not ElevatorDoor))
        {
            door.NetworkTargetState = false;
            door.ServerChangeLock(DoorLockReason.Warhead, false);
        }

        Warhead.OpenBlastDoors();
        Warhead.Scenario = Warhead.StartScenarios.First();
    }
}
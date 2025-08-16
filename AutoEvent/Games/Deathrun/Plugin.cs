﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using MEC;
using PlayerRoles;
using UnityEngine;
#if EXILED
using Player = Exiled.Events.Handlers.Player;
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Deathrun;

public class Plugin : Event<Config, Translation>, IEventMap
{
    private EventHandler _eventHandler;
    public override string Name { get; set; } = "Death Run";
    public override string Description { get; set; } = "Go to the end, avoiding death-activated trap along the way";
    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";
    public override string CommandName { get; set; } = "deathrun";
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll;
    private GameObject _wall { get; set; }
    private List<GameObject> _runnerSpawns { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "TempleMap",
        Position = new Vector3(0, 30, 30)
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler();
#if EXILED
        Player.SearchingPickup += _eventHandler.OnSearchingPickup;
#else
        PlayerEvents.SearchingPickup += _eventHandler.OnSearchingPickup;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Player.SearchingPickup -= _eventHandler.OnSearchingPickup;
#else
        PlayerEvents.SearchingPickup -= _eventHandler.OnSearchingPickup;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _runnerSpawns = new List<GameObject>();
        List<GameObject> deathSpawns = new();
        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Spawnpoint": _runnerSpawns.Add(block); break;
                case "Spawnpoint1": deathSpawns.Add(block); break;
                case "Wall": _wall = block; break;
                case "KillTrigger": block.AddComponent<KillComponent>(); break;
                case "ColliderTrigger": block.AddComponent<ColliderComponent>(); break;
                case "WeaponTrigger": block.AddComponent<WeaponComponent>().StartComponent(this); break;
                case "PoisonTrigger": block.AddComponent<PoisonComponent>().StartComponent(this); break;
            }

#if EXILED
        // Making a random death-guy and teleport to spawnpoint
        for (var i = 0; Player.List.Count() / 20 >= i; i++)
        {
            var death = Player.List.Where(r => r.Role != RoleTypeId.Scientist).ToList()
                .RandomItem();
            death.GiveLoadout(Config.DeathLoadouts);
            death.Position = deathSpawns.RandomItem().transform.position;
        }

        // Teleport runners to spawnpoint
        foreach (var runner in Player.List.Where(r => r.Role != RoleTypeId.Scientist))
        {
            runner.GiveLoadout(Config.PlayerLoadouts);
            runner.Position = _runnerSpawns.RandomItem().transform.position;
        }
#else
        // Making a random death-guy and teleport to spawnpoint
        for (var i = 0; Player.ReadyList.Count() / 20 >= i; i++)
        {
            var death = Player.ReadyList.Where(r => r.Role != RoleTypeId.Scientist).ToList().RandomItem();
            death.GiveLoadout(Config.DeathLoadouts);
            death.Position = deathSpawns.RandomItem().transform.position;
        }

        // Teleport runners to spawnpoint
        foreach (var runner in Player.ReadyList.Where(r => r.Role != RoleTypeId.Scientist))
        {
            runner.GiveLoadout(Config.PlayerLoadouts);
            runner.Position = _runnerSpawns.RandomItem().transform.position;
        }
#endif
    }

    // Counting down the time to the start of the game
    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 10; time > 0; time--)
        {
            var text = Translation.BeforeStartBroadcast.Replace("{name}", Name).Replace("{time}", $"{time}");
            Extensions.Broadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    // Destroy the wall so that players can start passing the map
    protected override void CountdownFinished()
    {
        _wall.transform.position += new Vector3(0, 10, 0);
    }

    // While all the players are alive and time has not over
    protected override bool IsRoundDone()
    {
        return !(Player.List.Count(r => r.Role == RoleTypeId.Scientist) > 0 &&
                 Player.List.Count(r => r.Role == RoleTypeId.ClassD) > 0 &&
                 EventTime.TotalSeconds < Config.RoundDurationInSeconds);
    }

    // All the logic of the game is handled by AMERT traps, so there is nothing here except the broadcast
    protected override void ProcessFrame()
    {
        var timeleft = TimeSpan.FromSeconds(Config.RoundDurationInSeconds - EventTime.TotalSeconds);
        var timetext = $"{timeleft.Minutes:00}:{timeleft.Seconds:00}";

        if (timeleft.TotalSeconds < 0)
        {
            timetext = Translation.OverTimeBroadcast;
#if EXILED
            foreach (var player in Player.List.Where(r => r.Role.Type is RoleTypeId.ClassD))
                if (player.Items.Count == 0)
                    player.Kill(Translation.Died);
        }
        // A second life for dead players
        else if (Config.SecondLifeInSeconds == EventTime.TotalSeconds)
        {
            foreach (var player in Player.List.Where(r => r.Role.Type is RoleTypeId.Spectator))
            {
                player.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.None);
                player.Position = _runnerSpawns.RandomItem().transform.position;
                player.ShowHint(Translation.SecondLifeHint, 5);
            }
        }

        var text = Translation.CycleBroadcast;
        text = text.Replace("{name}", Name);
        text = text.Replace("{runnerCount}",
            $"{Player.List.Count(r => r.Role.Type is RoleTypeId.ClassD)}");
        text = text.Replace("{deathCount}",
            $"{Player.List.Count(r => r.Role.Type is RoleTypeId.Scientist)}");
        text = text.Replace("{time}", timetext);
#else
            foreach (var player in Player.List.Where(r => r.Role is RoleTypeId.ClassD))
                if (!player.Items.Any())
                    player.Kill(Translation.Died);
        }
        // A second life for dead players
        else if (Config.SecondLifeInSeconds == EventTime.TotalSeconds)
        {
            foreach (var player in Player.ReadyList.Where(r => r.Role is RoleTypeId.Spectator))
            {
                player.SetRole(RoleTypeId.ClassD, flags: RoleSpawnFlags.None);
                player.Position = _runnerSpawns.RandomItem().transform.position;
                player.SendHint(Translation.SecondLifeHint, 5);
            }
        }

        var text = Translation.CycleBroadcast;
        text = text.Replace("{name}", Name);
        text = text.Replace("{runnerCount}", $"{Player.List.Count(r => r.Role is RoleTypeId.ClassD)}");
        text = text.Replace("{deathCount}", $"{Player.List.Count(r => r.Role is RoleTypeId.Scientist)}");
        text = text.Replace("{time}", timetext);
#endif


        Extensions.Broadcast(text, 1);
    }

    protected override void OnFinished()
    {
        var text = string.Empty;
#if EXILED
        if (Player.List.Count(r => r.Role.Type is RoleTypeId.ClassD) == 0)
#else
        if (Player.List.Count(r => r.Role is RoleTypeId.ClassD) == 0)
#endif
            text = Translation.DeathWinBroadcast.Replace("{name}", Name);
        else
            text = Translation.RunnerWinBroadcast.Replace("{name}", Name);

        Extensions.Broadcast(text, 10);
    }
}
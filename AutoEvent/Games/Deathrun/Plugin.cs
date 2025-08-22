using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public abstract class Plugin : Event<Config, Translation>, IEventMap
{
    public override string Name { get; set; } = "Death Run";
    public override string Description { get; set; } = "Go to the end, avoiding death-activated trap along the way";
    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";
    public override string CommandName { get; set; } = "deathrun";
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll;
    private GameObject Wall { get; set; }
    private List<GameObject> RunnerSpawns { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "TempleMap",
        Position = new Vector3(0, 30, 30)
    };

    protected override void RegisterEvents()
    {
        PlayerEvents.InteractedToy += EventHandler.OnPlayerInteractedToy;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.InteractedToy -= EventHandler.OnPlayerInteractedToy;
    }

    protected override void OnStart()
    {
        RunnerSpawns = [];
        List<GameObject> deathSpawns = [];
        foreach (var block in MapInfo.Map.AttachedBlocks)
            switch (block.name)
            {
                case "Spawnpoint": RunnerSpawns.Add(block); break;
                case "Spawnpoint1": deathSpawns.Add(block); break;
                case "Wall": Wall = block; break;
                case "KillTrigger": block.AddComponent<KillComponent>(); break;
                case "ColliderTrigger": block.AddComponent<ColliderComponent>(); break;
                case "WeaponTrigger": block.AddComponent<WeaponComponent>().StartComponent(this); break;
                case "PoisonTrigger": block.AddComponent<PoisonComponent>().StartComponent(this); break;
            }

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
            runner.Position = RunnerSpawns.RandomItem().transform.position;
        }
    }

    // Counting down the time to the start of the game
    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 10; time > 0; time--)
        {
            var text = Translation.BeforeStartBroadcast.Replace("{name}", Name).Replace("{time}", $"{time}");
            Extensions.ServerBroadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    // Destroy the wall so that players can start passing the map
    protected override void CountdownFinished()
    {
        Wall.transform.position += new Vector3(0, 10, 0);
    }

    // While all the players are alive and time has not over
    protected override bool IsRoundDone()
    {
        return !(Player.ReadyList.Count(r => r.Role == RoleTypeId.Scientist) > 0 &&
                 Player.ReadyList.Count(r => r.Role == RoleTypeId.ClassD) > 0 &&
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
            foreach (var player in Player.ReadyList.Where(r => r.Role is RoleTypeId.ClassD))
                if (!player.Items.Any())
                    player.Kill(Translation.Died);
        }
        // A second life for dead players
        else if (Config.SecondLifeInSeconds == EventTime.TotalSeconds)
        {
            foreach (var player in Player.ReadyList.Where(r => r.Role is RoleTypeId.Spectator))
            {
                player.SetRole(RoleTypeId.ClassD, flags: RoleSpawnFlags.None);
                player.Position = RunnerSpawns.RandomItem().transform.position;
                player.SendHint(Translation.SecondLifeHint, 5);
            }
        }

        var text = Translation.CycleBroadcast;
        text = text.Replace("{name}", Name);
        text = text.Replace("{runnerCount}", $"{Player.ReadyList.Count(r => r.Role is RoleTypeId.ClassD)}");
        text = text.Replace("{deathCount}", $"{Player.ReadyList.Count(r => r.Role is RoleTypeId.Scientist)}");
        text = text.Replace("{time}", timetext);


        Extensions.ServerBroadcast(text, 1);
    }

    protected override void OnFinished()
    {
        var text = Player.ReadyList.Count(r => r.Role is RoleTypeId.ClassD) == 0
            ? Translation.DeathWinBroadcast.Replace("{name}", Name)
            : Translation.RunnerWinBroadcast.Replace("{name}", Name);

        Extensions.ServerBroadcast(text, 10);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Games.CounterStrike.Features;
using AutoEvent.Interfaces;
using Footprinting;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.CounterStrike;

//todo: Make the bomb to be carryable, plantable anywhere on site and defusable
//todo: Make the bomb be on the player's back when carrying it
//todo: Dropped bomb model, CT can't pick it up.

public abstract class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    private EventHandler _eventHandler;
    internal GameObject BombObject;
    internal List<GameObject> BombPoints;
    internal BombState BombState;
    internal List<GameObject> Buttons;
    internal TimeSpan RoundTime;
    public override string Name { get; set; } = "Counter-Strike";
    public override string Description { get; set; } = "Fight between terrorists and counter-terrorists";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "cs";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.Default | EventFlags.IgnoreDroppingItem;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "de_dust2",
        Position = new Vector3(0, 30, 30)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Survival.ogg",
        Volume = 10,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.SearchingPickup += _eventHandler.OnSearchingPickup;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.SearchingPickup -= _eventHandler.OnSearchingPickup;
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        BombObject = new GameObject();
        Buttons = [];
        BombState = BombState.NoPlanted;
        RoundTime = new TimeSpan(0, 0, Config.TotalTimeInSeconds);
        List<GameObject> ctSpawn = [];
        List<GameObject> tSpawn = [];

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint_Counter": ctSpawn.Add(gameObject); break;
                case "Spawnpoint_Terrorist": tSpawn.Add(gameObject); break;
                case "Bomb": BombObject = gameObject; break;
                case "Spawnpoint_Bomb": Buttons.Add(gameObject); break;
            }

        var count = 0;
        foreach (var player in Player.ReadyList)
        {
            if (count % 2 == 0)
            {
                player.GiveLoadout(Config.NtfLoadouts);
                player.Position = ctSpawn.RandomItem().transform.position +
                                  new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            }
            else
            {
                player.GiveLoadout(Config.ChaosLoadouts);
                player.Position = tSpawn.RandomItem().transform.position +
                                  new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            }

            count++;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 20; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        // We are removing the walls so that the players can walk.
        MapInfo.Map.AttachedBlocks.Where(r => r.name == "Wall").ToList()
            .ForEach(Object.Destroy);
    }

    protected override bool IsRoundDone()
    {
        var ctCount = Player.ReadyList.Count(r => r.IsNTF);
        var tCount = Player.ReadyList.Count(r => r.IsChaos);

        return !((tCount > 0 || BombState == BombState.Planted) &&
                 ctCount > 0 &&
                 RoundTime.TotalSeconds != 0);
    }

    protected override void ProcessFrame()
    {
        var ctCount = Player.ReadyList.Count(r => r.IsNTF);
        var tCount = Player.ReadyList.Count(r => r.IsChaos);
        var time = $"{RoundTime.Minutes:00}:{RoundTime.Seconds:00}";

        // Counts the time until the end of the round and changes according to the actions of the players
        TimeCounter();

        // Shows all players their missions
        var ctTask = string.Empty;
        var tTask = string.Empty;
        if (BombState == BombState.NoPlanted)
        {
            ctTask = Translation.NoPlantedCounter;
            tTask = Translation.NoPlantedTerror;
        }
        else if (BombState == BombState.Planted)
        {
            ctTask = Translation.PlantedCounter;
            tTask = Translation.PlantedTerror;
        }

        // Output of missions to broadcast and killboard to hints
        foreach (var player in Player.ReadyList)
        {
            var text = Translation.Cycle.Replace("{name}", Name)
                .Replace("{task}", player.Role == RoleTypeId.NtfSpecialist ? ctTask : tTask)
                .Replace("{ctCount}", ctCount.ToString()).Replace("{tCount}", tCount.ToString())
                .Replace("{time}", time);

            player.Broadcast(text, 1);
        }
    }

    protected void TimeCounter()
    {
        RoundTime -= TimeSpan.FromSeconds(1);

        if (BombState == BombState.Planted)
        {
            if (RoundTime.TotalSeconds == 0) BombState = BombState.Exploded;
        }
        else if (BombState == BombState.Defused)
        {
            RoundTime = new TimeSpan(0, 0, 0);
        }
    }

    protected override void OnFinished()
    {
        var ctCount = Player.ReadyList.Count(r => r.IsNTF);
        var tCount = Player.ReadyList.Count(r => r.IsChaos);

        string text;
        if (BombState == BombState.Exploded)
        {
            foreach (var player in Player.ReadyList)
                if (player.IsAlive)
                    player.Damage(new ExplosionDamageHandler(new Footprint(Player.Host?.ReferenceHub), Vector3.back,
                        1000, 100, ExplosionType.Grenade));

            text = Translation.PlantedWin;
            Extensions.PlayAudio("TBombWin.ogg", 15, false);
        }
        else if (BombState == BombState.Defused)
        {
            text = Translation.DefusedWin;
            Extensions.PlayAudio("CTWin.ogg", 10, false);
        }
        else if (tCount == 0 && ctCount > 0)
        {
            text = Translation.CounterWin;
            Extensions.PlayAudio("CTWin.ogg", 10, false);
        }
        else if (ctCount == 0 && tCount > 0)
        {
            text = Translation.TerroristWin;
            Extensions.PlayAudio("TWin.ogg", 15, false);
        }
        else if (ctCount == 0 && tCount == 0)
        {
            text = Translation.Draw;
        }
        else
        {
            text = Translation.TimeEnded;
        }

        Extensions.ServerBroadcast(text, 10);
    }
}
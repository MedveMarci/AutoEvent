using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Games.CounterStrike.Features;
using AutoEvent.Interfaces;
using Footprinting;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using Extensions = AutoEvent.API.Extensions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.CounterStrike;

public class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    private EventHandler _eventHandler;
    internal AdminToyBase BombObject;
    internal BombState BombState;
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
        PlayerEvents.SearchedToy += _eventHandler.OnSearchedToy;
        PlayerEvents.SearchingToy += EventHandler.OnSearchingToy;
        PlayerEvents.SearchToyAborted += EventHandler.OnSearchToyAborted;
        PlayerEvents.UsingItem += EventHandler.OnUsingItem;
        PlayerEvents.UsedItem += _eventHandler.OnUsedItem;
        PlayerEvents.PickingUpItem += _eventHandler.OnPickingUpItem;
        PlayerEvents.ChangedItem += EventHandler.OnChangedItemEvent;
        PlayerEvents.CancelledUsingItem += EventHandler.OnCancelledUsingItem;
        PlayerEvents.DroppedItem += _eventHandler.OnDroppedItem;
        PlayerEvents.SearchingPickup += EventHandler.OnSearchingPickup;
        
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.SearchedToy -= _eventHandler.OnSearchedToy;
        PlayerEvents.UsingItem -= EventHandler.OnUsingItem;
        PlayerEvents.UsedItem -= _eventHandler.OnUsedItem;
        PlayerEvents.PickingUpItem -= _eventHandler.OnPickingUpItem;
        PlayerEvents.CancelledUsingItem -= EventHandler.OnCancelledUsingItem;
        PlayerEvents.ChangedItem -= EventHandler.OnChangedItemEvent;
        PlayerEvents.SearchingToy += EventHandler.OnSearchingToy;
        PlayerEvents.SearchToyAborted += EventHandler.OnSearchToyAborted;
        PlayerEvents.DroppedItem -= _eventHandler.OnDroppedItem;
        PlayerEvents.SearchingPickup -= EventHandler.OnSearchingPickup;
        
        
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        BombState = BombState.NoPlanted;
        RoundTime = new TimeSpan(0, 0, Config.TotalTimeInSeconds);
        List<GameObject> ctSpawn = [];
        List<GameObject> tSpawn = [];

        foreach (var gameObject in MapInfo.Map.AdminToyBases)
            switch (gameObject.name)
            {
                case "Spawnpoint_Counter": ctSpawn.Add(gameObject.gameObject); break;
                case "Spawnpoint_Terrorist": tSpawn.Add(gameObject.gameObject); break;
                case "Bomb": BombObject = gameObject; break;
                case "ASiteBounds":
                    EventHandler.ASiteBounds = new Bounds
                    {
                        center = gameObject.gameObject.transform.position,
                        size = gameObject.gameObject.transform.localScale
                    };
                    break;
                case "BSiteBounds":
                    EventHandler.BSiteBounds = new Bounds
                    {
                        center = gameObject.gameObject.transform.position,
                        size = gameObject.gameObject.transform.localScale
                    };
                    break;
                case "Bomb_Button":
                    EventHandler.Button = InteractableToy.Get((InvisibleInteractableToy)gameObject);
                    break;
            }

        var shuffledPlayers = Player.ReadyList.OrderBy(_ => Random.value).ToList();
        var count = 0;
        foreach (var player in shuffledPlayers)
        {
            if (count % 2 != 0)
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
                if (EventHandler.Bomb == null)
                {
                    EventHandler.Bomb = (LabApi.Features.Wrappers.Scp1576Item)player.AddItem(ItemType.SCP1576);
                    player.SendHint(Translation.PickedUpBomb);
                    BombObject.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    BombObject.gameObject.transform.parent = player.GameObject.transform;
                    BombObject.gameObject.transform.localPosition = new Vector3(0, 0.27f, -0.263f);
                    BombObject.gameObject.transform.localRotation = new Quaternion(-0.707106829f, 0, 0, 0.707106829f);
                }
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

    protected override void OnCleanup()
    {
        NetworkServer.Destroy(BombObject.gameObject);
        BombObject = null;
        EventHandler.Bomb = null;
        base.OnCleanup();
    }
}
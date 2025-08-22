using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Survival;

public abstract class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _remainingTime;
    private GameObject _teleport;
    private GameObject _teleport1;
    internal Player FirstZombie;
    internal List<GameObject> SpawnList;
    public override string Name { get; set; } = "Zombie Survival";
    public override string Description { get; set; } = "Humans surviving from zombies";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "zombie2";
    private EventHandler EventHandler { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Survival",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Survival.ogg",
        Volume = 10,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        EventHandler = new EventHandler(this);
        PlayerEvents.Joined += EventHandler.OnJoined;
        PlayerEvents.Dying += EventHandler.OnDying;
        PlayerEvents.Hurting += EventHandler.OnHurting;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Joined -= EventHandler.OnJoined;
        PlayerEvents.Dying -= EventHandler.OnDying;
        PlayerEvents.Hurting -= EventHandler.OnHurting;

        EventHandler = null;
    }

    protected override void OnStart()
    {
        _remainingTime = new TimeSpan(0, 5, 0);
        Server.FriendlyFire = false;

        SpawnList = MapInfo.Map.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList();
        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.PlayerLoadouts);
            player.Position = SpawnList.RandomItem().transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 20; time > 0; time--)
        {
            Extensions.ServerBroadcast(
                Translation.SurvivalBeforeInfection.Replace("{name}", Name).Replace("{time}", $"{time}"), 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        Extensions.PlayAudio("Zombie2.ogg", 7, true);

        var players = Config.Zombies.GetPlayers();
        foreach (var player in players)
        {
            LogManager.Debug($"Making player {player.Nickname} a zombie.");
            player.GiveLoadout(Config.ZombieLoadouts);
            Extensions.PlayPlayerAudio(SoundInfo.AudioPlayer, player, Config.ZombieScreams.RandomItem(), 15);
            if (Player.ReadyList.Count(r => r.IsSCP) != 1) continue;
            if (FirstZombie is not null)
                continue;

            FirstZombie = player;
        }

        _teleport = MapInfo.Map.AttachedBlocks.First(x => x.name == "Teleport");
        _teleport1 = MapInfo.Map.AttachedBlocks.First(x => x.name == "Teleport1");
    }

    protected override bool IsRoundDone()
    {
        // At least 1 human player &&
        // At least 1 scp player &&
        // round time under 5 minutes (+ countdown)
        var a = Player.ReadyList.Any(ply => ply.HasLoadout(Config.PlayerLoadouts));
        var b = Player.ReadyList.Any(ply => ply.HasLoadout(Config.ZombieLoadouts));
        var c = EventTime.TotalSeconds < Config.RoundDurationInSeconds;
        return !(a && b && c);
    }

    protected override void ProcessFrame()
    {
        var text = Translation.SurvivalAfterInfection;

        text = text.Replace("{name}", Name);
        text = text.Replace("{humanCount}", Player.ReadyList.Count(r => r.IsHuman).ToString());
        text = text.Replace("{time}", $"{_remainingTime.Minutes:00}:{_remainingTime.Seconds:00}");

        foreach (var player in Player.ReadyList)
        {
            player.ClearBroadcasts();
            player.Broadcast(text, 1);

            if (Vector3.Distance(player.Position, _teleport.transform.position) < 1)
                player.Position = _teleport1.transform.position;
        }

        _remainingTime -= TimeSpan.FromSeconds(FrameDelayInSeconds);
    }

    protected override void OnFinished()
    {
        string text;
        var musicName = "HumanWin.ogg";

        if (Player.ReadyList.Count(r => r.IsHuman) == 0)
        {
            text = Translation.SurvivalZombieWin;
            musicName = "ZombieWin.ogg";
        }
        else if (Player.ReadyList.Count(r => r.IsSCP) == 0)

        {
            text = Translation.SurvivalHumanWin;
        }
        else
        {
            text = Translation.SurvivalHumanWinTime;
        }

        Extensions.PauseAudio(SoundInfo.AudioPlayer);
        Extensions.PlayAudio(musicName, 7, false);
        Extensions.ServerBroadcast(text, 10);
    }
}
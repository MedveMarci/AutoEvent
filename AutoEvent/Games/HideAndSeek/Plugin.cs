﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.HideAndSeek;

public abstract class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private TimeSpan _countdown;
    private EventHandler _eventHandler;
    private EventState _eventState;
    public override string Name { get; set; } = "Tag";
    public override string Description { get; set; } = "We need to catch up with all the players on the map";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "tag";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "HideAndSeek",
        Position = new Vector3(0, 30, 30)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "HideAndSeek.ogg",
        Volume = 5
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        PlayerEvents.Hurting += _eventHandler.OnHurting;
        PlayerEvents.ProcessingJailbirdMessage += _eventHandler.OnJailbirdCharge;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Hurting -= _eventHandler.OnHurting;
        PlayerEvents.ProcessingJailbirdMessage -= _eventHandler.OnJailbirdCharge;
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _eventState = 0;
        var spawnpoints = MapInfo.Map.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList();
        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.PlayerLoadouts);
            player.Position = spawnpoints.RandomItem().transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 15; time > 0; time--)
        {
            Extensions.ServerBroadcast(Translation.Broadcast.Replace("{time}", $"{time}"), 1);

            yield return Timing.WaitForSeconds(1f);
            EventTime += TimeSpan.FromSeconds(1f);
        }
    }

    protected override bool IsRoundDone()
    {
        _countdown = _countdown.TotalSeconds > 0 ? _countdown.Subtract(new TimeSpan(0, 0, 1)) : TimeSpan.Zero;
        return !(Player.ReadyList.Count(ply => ply.IsAlive) > 1);
    }

    protected override void ProcessFrame()
    {
        var text = string.Empty;
        switch (_eventState)
        {
            case EventState.SelectPlayers: SelectPlayers(ref text); break;
            case EventState.TagPeriod: UpdateTagPeriod(ref text); break;
            case EventState.KillTaggers: KillTaggers(ref text); break;
            case EventState.PlayerBreak: UpdatePlayerBreak(ref text); break;
        }

        Extensions.ServerBroadcast(text, 1);
    }

    /// <summary>
    ///     Choosing the player(s) who will catch up with other players
    /// </summary>
    protected void SelectPlayers(ref string text)
    {
        text = Translation.Broadcast.Replace("{time}", $"{_countdown.TotalSeconds}");
        var playersToChoose = Player.ReadyList.Where(x => x.IsAlive).ToList();
        foreach (var ply in Config.TaggerCount.GetPlayers(true, playersToChoose))
        {
            ply.GiveLoadout(Config.TaggerLoadouts);
            ply.CurrentItem ??= ply.AddItem(Config.TaggerWeapon);
        }

        if (Player.ReadyList.Count(ply => ply.HasLoadout(Config.PlayerLoadouts)) <=
            Config.PlayersRequiredForBreachScannerEffect)
            foreach (var player in Player.ReadyList.Where(ply => ply.HasLoadout(Config.PlayerLoadouts)))
                player.EnableEffect<Scanned>(255);

        _countdown = new TimeSpan(0, 0, Config.TagDuration);
        _eventState++;
    }

    /// <summary>
    ///     Just waiting N seconds until the time runs out
    /// </summary>
    protected void UpdateTagPeriod(ref string text)
    {
        text = Translation.Cycle.Replace("{time}", $"{_countdown.TotalSeconds}");

        if (_countdown.TotalSeconds <= 0)
            _eventState++;
    }

    /// <summary>
    ///     Kill players who are taggers.
    /// </summary>
    protected void KillTaggers(ref string text)
    {
        text = Translation.Cycle.Replace("{time}", $"{_countdown.TotalSeconds}");

        foreach (var player in Player.ReadyList)
            if (player.Items.Any(r => r.Type == Config.TaggerWeapon))
            {
                player.ClearInventory();
                player.Damage(200, Translation.Hurt);
            }

        _countdown = new TimeSpan(0, 0, Config.BreakDuration);
        _eventState++;
    }

    /// <summary>
    ///     Wait for N seconds before choosing next batch.
    /// </summary>
    protected void UpdatePlayerBreak(ref string text)
    {
        text = Translation.Broadcast.Replace("{time}", $"{_countdown.TotalSeconds}");

        if (_countdown.TotalSeconds <= 0)
            _eventState = 0;
    }

    protected override void OnFinished()
    {
        string text;
        if (Player.ReadyList.Count(r => r.IsAlive) >= 1)
            text = Translation.OnePlayer
                .Replace("{winner}", Player.ReadyList.First(r => r.IsAlive).Nickname)
                .Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}");
        else
            text = Translation.AllDie.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}");

        Extensions.ServerBroadcast(text, 10);
    }
}
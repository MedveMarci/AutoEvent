using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using MEC;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.AllDeathmatch;

public class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    internal Dictionary<Player, int> TotalKills;
    public override string Name { get; set; } = "All Deathmatch";
    public override string Description { get; set; } = "Fight against each other in all deathmatch.";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "dm";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreDroppingItem;
    private EventHandler _eventHandler { get; set; }
    private int _needKills { get; set; }
    private Player _winner { get; set; }
    internal List<GameObject> SpawnList { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "de_dust2",
        Position = new Vector3(0, 30, 30)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "ExecDeathmatch.ogg",
        Volume = 10
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Exiled.Events.Handlers.Player.Dying += _eventHandler.OnPlayerDying;
        Exiled.Events.Handlers.Player.Joined += _eventHandler.OnJoined;
        Exiled.Events.Handlers.Player.Left += _eventHandler.OnLeft;
#else
        PlayerEvents.Dying += _eventHandler.OnPlayerDying;
        PlayerEvents.Joined += _eventHandler.OnJoined;
        PlayerEvents.Left += _eventHandler.OnLeft;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Exiled.Events.Handlers.Player.Dying -= _eventHandler.OnPlayerDying;
        Exiled.Events.Handlers.Player.Joined -= _eventHandler.OnJoined;
        Exiled.Events.Handlers.Player.Left -= _eventHandler.OnLeft;
#else
        PlayerEvents.Dying -= _eventHandler.OnPlayerDying;
        PlayerEvents.Joined -= _eventHandler.OnJoined;
        PlayerEvents.Left -= _eventHandler.OnLeft;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _winner = null;
        _needKills = 0;
        TotalKills = new Dictionary<Player, int>();
        SpawnList = new List<GameObject>();
#if EXILED
        switch (Player.List.Count)
#else
        switch (Player.ReadyList.Count())
#endif
        {
            case int n when n > 0 && n <= 5: _needKills = 10; break;
            case int n when n > 5 && n <= 10: _needKills = 15; break;
            case int n when n > 10 && n <= 20: _needKills = 25; break;
            case int n when n > 20 && n <= 25: _needKills = 50; break;
            case int n when n > 25 && n <= 35: _needKills = 75; break;
            case int n when n > 35: _needKills = 100; break;
        }

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint_Deathmatch": SpawnList.Add(gameObject); break;
                case "Wall": GameObject.Destroy(gameObject); break;
            }

#if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            player.GiveLoadout(Config.NTFLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
            player.Position = SpawnList.RandomItem().transform.position;

            TotalKills.Add(player, 0);
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.Broadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
#if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
            if (player.CurrentItem == null)
                player.CurrentItem = player.AddItem(Config.AvailableWeapons.RandomItem());
    }

    protected override bool IsRoundDone()
    {
        return !(Config.TimeMinutesRound >= EventTime.TotalMinutes &&
#if EXILED
                 Player.List.Count(r => r.IsAlive) > 1 && _winner is null);
#else
                 Player.ReadyList.Count(r => r.IsAlive) > 1 && _winner is null);
#endif
    }

    protected override void ProcessFrame()
    {
        var remainTime = Config.TimeMinutesRound - EventTime.TotalMinutes;
        var time = $"{(int)remainTime:00}:{(int)(remainTime * 60 % 60):00}";
        var sortedDict = TotalKills.OrderByDescending(r => r.Value).ToDictionary(x => x.Key, x => x.Value);

        var leaderboard = new StringBuilder("Leaderboard:\n");
        for (var i = 0; i < 3; i++)
            if (i < sortedDict.Count)
            {
                var color = string.Empty;
                switch (i)
                {
                    case 0: color = "#ffd700"; break;
                    case 1: color = "#c0c0c0"; break;
                    case 2: color = "#cd7f32"; break;
                }

                var length = Math.Min(sortedDict.ElementAt(i).Key.Nickname.Length, 10);
                leaderboard.Append($"<color={color}>{i + 1}. ");
                leaderboard.Append($"{sortedDict.ElementAt(i).Key.Nickname.Substring(0, length)} ");
                leaderboard.Append($"/ {sortedDict.ElementAt(i).Value} kills</color>\n");
            }
#if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            var playerText = string.Empty;

            if (TotalKills[player] >= _needKills) _winner = player;

            var playerItem = sortedDict.FirstOrDefault(x => x.Key == player);
            playerText = leaderboard + $"<color=#ff0000>You - {playerItem.Value}/{_needKills} kills</color></size>";

            var text = Translation.Cycle.Replace("{name}", Name).Replace("{kills}", playerItem.Value.ToString())
                .Replace("{needKills}", _needKills.ToString()).Replace("{time}", time);

            player.ClearBroadcasts();
#if EXILED
            player.ShowHint($"<line-height=95%><voffset=25em><align=right><size=30>{playerText}</size></align>", 1);
            player.Broadcast(1, text);
#else
            player.SendHint($"<line-height=95%><voffset=25em><align=right><size=30>{playerText}</size></align>", 1);
            player.SendBroadcast(text, 1);
#endif
        }
    }

    protected override void OnFinished()
    {
        var time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
#if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            var text = string.Empty;
#if EXILED
            if (Player.List.Count(r => r.IsAlive) <= 1)
#else
            if (Player.ReadyList.Count(r => r.IsAlive) <= 1)
#endif
                text = Translation.NoPlayers;
            else if (EventTime.TotalMinutes >= Config.TimeMinutesRound)
                text = Translation.TimeEnd;
            else if (_winner != null)
                text = Translation.WinnerEnd.Replace("{winner}", _winner.Nickname).Replace("{time}", time);

            text = text.Replace("{count}", TotalKills.First(x => x.Key == player).Value.ToString());
            player.ClearBroadcasts();
#if EXILED
            player.Broadcast(10, text);
#else
            player.SendBroadcast(text, 10);
#endif
        }
    }
}
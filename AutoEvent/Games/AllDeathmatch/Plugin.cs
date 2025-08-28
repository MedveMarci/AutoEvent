using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Games.AllDeathmatch.Configs;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using UnityEngine;
using Extensions = AutoEvent.API.Extensions;
using Object = UnityEngine.Object;

namespace AutoEvent.Games.AllDeathmatch;

public class Plugin : Event<Configs.Config, Translation>, IEventMap, IEventSound
{
    internal Dictionary<string, int> TotalKills;
    public override string Name { get; set; } = "All Deathmatch";
    public override string Description { get; set; } = "Fight against each other in all deathmatch.";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "dm";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreDroppingItem;
    private EventHandler EventHandler { get; set; }
    private int NeedKills { get; set; }
    private Player Winner { get; set; }
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
        EventHandler = new EventHandler(this);
        PlayerEvents.Dying += EventHandler.OnPlayerDying;
        PlayerEvents.Joined += EventHandler.OnJoined;
        PlayerEvents.Left += EventHandler.OnLeft;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Dying -= EventHandler.OnPlayerDying;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        PlayerEvents.Left -= EventHandler.OnLeft;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        Winner = null;
        NeedKills = 0;
        TotalKills = new Dictionary<string, int>();
        SpawnList = [];
        NeedKills = Player.ReadyList.Count() switch
        {
            <= 5 and > 0 => 10,
            <= 10 and > 5 => 15,
            <= 20 and > 10 => 25,
            <= 25 and > 20 => 50,
            <= 35 and > 25 => 75,
            > 35 => 100,
            _ => NeedKills
        };

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint_Deathmatch": SpawnList.Add(gameObject); break;
                case "Wall": NetworkServer.Destroy(gameObject);; break;
            }

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.NtfLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
            player.Position = SpawnList.RandomItem().transform.position;

            TotalKills.Add(player.UserId, 0);
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        foreach (var player in Player.ReadyList)
            player.CurrentItem ??= player.AddItem(Config.AvailableWeapons.RandomItem());
    }

    protected override bool IsRoundDone()
    {
        return !(Config.TimeMinutesRound >= EventTime.TotalMinutes &&
                 Player.ReadyList.Count(r => r.IsAlive) > 1 && Winner is null);
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
                var color = i switch
                {
                    0 => "#ffd700",
                    1 => "#c0c0c0",
                    2 => "#cd7f32",
                    _ => string.Empty
                };
                var player = Player.Get(sortedDict.ElementAt(i).Key);
                if (player is null) continue;
                var length = Math.Min(player.Nickname.Length, 10);
                leaderboard.Append($"<color={color}>{i + 1}. ");
                leaderboard.Append($"{player.Nickname.Substring(0, length)} ");
                leaderboard.Append($"/ {sortedDict.ElementAt(i).Value} kills</color>\n");
            }

        foreach (var player in Player.ReadyList)
        {
            if (!TotalKills.ContainsKey(player.UserId))
                TotalKills.Add(player.UserId, 0);

            if (TotalKills[player.UserId] >= NeedKills) Winner = player;

            var playerItem = sortedDict.FirstOrDefault(x => x.Key == player.UserId);
            var playerText = leaderboard + $"<color=#ff0000>You - {playerItem.Value}/{NeedKills} kills</color></size>";

            var text = Translation.Cycle.Replace("{name}", Name).Replace("{kills}", playerItem.Value.ToString())
                .Replace("{needKills}", NeedKills.ToString()).Replace("{time}", time);
            player.SendHint($"<line-height=95%><voffset=25em><align=right><size=30>{playerText}</size></align>", 1);
            player.Broadcast(text, 1);
        }
    }

    protected override void OnFinished()
    {
        var time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
        foreach (var player in Player.ReadyList)
        {
            var text = string.Empty;
            if (Player.ReadyList.Count(r => r.IsAlive) <= 1)
                text = Translation.NoPlayers;
            else if (EventTime.TotalMinutes >= Config.TimeMinutesRound)
                text = Translation.TimeEnd;
            else if (Winner != null)
                text = Translation.WinnerEnd.Replace("{winner}", Winner.Nickname).Replace("{time}", time);

            text = text.Replace("{count}", TotalKills.First(x => x.Key == player.UserId).Value.ToString());
            Extensions.ServerBroadcast(text, 10);
        }
    }
}
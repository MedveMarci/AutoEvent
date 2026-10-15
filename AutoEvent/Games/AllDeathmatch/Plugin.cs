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

namespace AutoEvent.Games.AllDeathmatch;

public class Plugin : Event<Configs.Config, Translation>, IEventMap, IEventSound
{
    internal Dictionary<uint, int> TotalKills;

    public override string Name { get; set; } = "All Deathmatch";

    public override string Description { get; set; } = "Fight against each other in all deathmatch.";

    public override string Author { get; set; } = "RisottoMan";

    public override string CommandName { get; set; } = "dm";

    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll | EventFlags.IgnoreHandcuffing | EventFlags.IgnoreBulletHole | EventFlags.IgnoreBloodDecal | EventFlags.IgnoreDroppingItem;

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
        SoundName = "ExecDeathmatch.ogg"
    };

    protected override void RegisterEvents()
    {
        EventHandler = new EventHandler(this);
        PlayerEvents.Dying += EventHandler.OnPlayerDying;
        PlayerEvents.Joined += EventHandler.OnJoined;
        PlayerEvents.Left += EventHandler.OnLeft;
        PlayerEvents.PlacingBlood += EventHandler.OnPlacingBlood;
        PlayerEvents.Cuffing += EventHandler.OnCuffing;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Dying -= EventHandler.OnPlayerDying;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        PlayerEvents.Left -= EventHandler.OnLeft;
        PlayerEvents.PlacingBlood -= EventHandler.OnPlacingBlood;
        PlayerEvents.Cuffing -= EventHandler.OnCuffing;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        Winner = null;
        NeedKills = 0;
        TotalKills = new Dictionary<uint, int>();
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

        foreach (GameObject gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint_Deathmatch":
                case "Spawnpoint": SpawnList.Add(gameObject); break;
                case "Wall":
                    NetworkServer.Destroy(gameObject);
                    break;
            }

        foreach (Player player in Player.ReadyList)
        {
            player.GiveLoadout(Config.NtfLoadouts, LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
            player.Position = SpawnList.RandomItem().transform.position;

            if (!TotalKills.ContainsKey(player.NetworkId))
                TotalKills.Add(player.NetworkId, 0);
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (int time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        foreach (Player player in Player.ReadyList)
            player.CurrentItem ??= player.AddItem(Config.AvailableWeapons.RandomItem());
    }

    protected override bool IsRoundDone()
    {
        return !(Config.TimeMinutesRound >= EventTime.TotalMinutes && Player.ReadyList.Count(r => r.IsAlive) > 1 && Winner is null);
    }

    protected override void ProcessFrame()
    {
        double remainTime = Config.TimeMinutesRound - EventTime.TotalMinutes;
        string time = $"{(int)remainTime:00}:{(int)(remainTime * 60 % 60):00}";
        Dictionary<uint, int> sortedDict = TotalKills.OrderByDescending(r => r.Value).ToDictionary(x => x.Key, x => x.Value);

        StringBuilder leaderboard = new($"{Translation.Leaderboard}:\n");
        for (int i = 0; i < 3; i++)
            if (i < sortedDict.Count)
            {
                string color = i switch
                {
                    0 => "#ffd700",
                    1 => "#c0c0c0",
                    2 => "#cd7f32",
                    _ => string.Empty
                };
                Player player = Player.Get(sortedDict.ElementAt(i).Key);
                if (player is null) continue;
                int length = Math.Min(player.Nickname.Length, 10);
                leaderboard.Append(Translation.LeaderboardContent.Replace("{color}", color)).Replace("{num}", (i + 1).ToString()).Replace("{playerName}", player.Nickname.Substring(0, length)).Replace("{kills}", sortedDict.ElementAt(i).Value.ToString());
                leaderboard.Append("\n");
            }

        foreach (Player player in Player.ReadyList)
        {
            if (!TotalKills.ContainsKey(player.NetworkId))
                TotalKills.Add(player.NetworkId, 0);

            if (TotalKills[player.NetworkId] >= NeedKills) Winner = player;

            KeyValuePair<uint, int> playerItem = sortedDict.FirstOrDefault(x => x.Key == player.NetworkId);
            string playerText = leaderboard + Translation.HintCycle.Replace("{kills}", playerItem.Value.ToString()).Replace("{needKills}", NeedKills.ToString());

            string text = Translation.Cycle.Replace("{name}", Name).Replace("{kills}", playerItem.Value.ToString()).Replace("{needKills}", NeedKills.ToString()).Replace("{time}", time);
            player.SendHint($"<line-height=95%><voffset=25em><align=right><size=30>{playerText}</size></align>", 1);
            player.Broadcast(text, 1);
        }
    }

    protected override void OnFinished()
    {
        string time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
        foreach (Player player in Player.ReadyList)
        {
            string text = string.Empty;
            if (Player.ReadyList.Count(r => r.IsAlive) <= 1)
                text = Translation.NoPlayers;
            else if (EventTime.TotalMinutes >= Config.TimeMinutesRound)
                text = Translation.TimeEnd;
            else if (Winner != null)
                text = Translation.WinnerEnd.Replace("{winner}", Winner.Nickname).Replace("{time}", time);

            text = text.Replace("{count}", TotalKills.TryGetValue(player.NetworkId, out int kills) ? kills.ToString() : "0");
            player.Broadcast(text, 10);
        }
    }
}
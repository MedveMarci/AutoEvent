using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using AutoEvent.Events;
using AutoEvent.Interfaces;
using InventorySystem;
using MEC;
using PlayerRoles;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
#endif

namespace AutoEvent.Games.Dodgeball;

public class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    private List<GameObject> _ballItems;
    private ItemType _ballItemType;
    private List<GameObject> _dPoint;
    private EventHandler _eventHandler;
    private GameObject _redLine;
    private TimeSpan _roundTime;
    private List<GameObject> _sciPoint;
    private List<GameObject> _walls;
    public override string Name { get; set; } = "Dodgeball";
    public override string Description { get; set; } = "Defeat the enemy with balls.";
    public override string Author { get; set; } = "RisottoMan & Моге-ко";
    public override string CommandName { get; set; } = "dodge";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;
    internal bool IsChristmasUpdate { get; set; }
    protected override float FrameDelayInSeconds { get; set; } = 0.1f;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Dodgeball",
        Position = new Vector3(0, 0, 30)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Fall_Guys_Winter_Fallympics.ogg",
        Volume = 7
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
        Handlers.Scp018Update += _eventHandler.OnScp018Update;
        Handlers.Scp018Collision += _eventHandler.OnScp018Collision;
#if EXILED
        Exiled.Events.Handlers.Player.Hurting += _eventHandler.OnHurting;
#else
        PlayerEvents.Hurting += _eventHandler.OnHurting;
#endif
    }

    protected override void UnregisterEvents()
    {
        Handlers.Scp018Update -= _eventHandler.OnScp018Update;
        Handlers.Scp018Collision -= _eventHandler.OnScp018Collision;
#if EXILED
        Exiled.Events.Handlers.Player.Hurting -= _eventHandler.OnHurting;
#else
        PlayerEvents.Hurting -= _eventHandler.OnHurting;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _redLine = null;
        _walls = new List<GameObject>();
        _ballItems = new List<GameObject>();
        _dPoint = new List<GameObject>();
        _sciPoint = new List<GameObject>();
        _roundTime = new TimeSpan(0, 0, Config.TotalTimeInSeconds);
        _ballItemType = ItemType.SCP018;

        // Christmas update -> check that the snowball item exists and not null
        if (Enum.TryParse("Snowball", out ItemType snowItemType))
        {
            InventoryItemLoader.AvailableItems.TryGetValue(snowItemType, out var itemBase);
            if (itemBase != null)
            {
                IsChristmasUpdate = true;
                _ballItemType = snowItemType;
            }
        }

        foreach (var gameObject in MapInfo.Map.AttachedBlocks)
            switch (gameObject.name)
            {
                case "Spawnpoint_ClassD": _dPoint.Add(gameObject); break;
                case "Spawnpoint_Scientist": _sciPoint.Add(gameObject); break;
                case "Wall": _walls.Add(gameObject); break;
                case "Snowball_Item": _ballItems.Add(gameObject); break;
                case "RedLine": _redLine = gameObject; break;
            }

        var count = 0;
        #if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            if (count % 2 == 0)
            {
                player.GiveLoadout(Config.ClassDLoadouts);
                player.Position = _dPoint.RandomItem().transform.position;
            }
            else
            {
                player.GiveLoadout(Config.ScientistLoadouts);
                player.Position = _sciPoint.RandomItem().transform.position;
            }

            count++;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            var text = Translation.Start.Replace("{time}", time.ToString());
            Extensions.Broadcast(text, 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        foreach (var wall in _walls) GameObject.Destroy(wall);
    }

    protected override bool IsRoundDone()
    {
        _roundTime -= TimeSpan.FromSeconds(0.1f);
        return !(_roundTime.TotalSeconds > 0 &&
                 Player.List.Count(r => r.Role == RoleTypeId.ClassD) > 0 &&
                 Player.List.Count(r => r.Role == RoleTypeId.Scientist) > 0);
    }

    protected override void ProcessFrame()
    {
        var time = $"{_roundTime.Minutes:00}:{_roundTime.Seconds:00}";
        var text = Translation.Cycle.Replace("{name}", Name).Replace("{time}", time);

        #if EXILED
        foreach (var player in Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            // If a player tries to go to the other half of the field, he takes damage and teleports him back
            if (Mathf.Approximately((int)_redLine.transform.position.z, (int)player.Position.z))
            {
                if (player.Role == RoleTypeId.ClassD)
                    player.Position = _dPoint.RandomItem().transform.position;
                else
                    player.Position = _sciPoint.RandomItem().transform.position;
#if EXILED
                player.Hurt(40, Translation.Redline);
#else
                player.Damage(40, Translation.Redline);
#endif
            }

            // If a player approaches the balls, then the ball is given into his hand
            foreach (var ball in _ballItems)
                if (Vector3.Distance(ball.transform.position, player.Position) < 1.5f)
                    if (player.CurrentItem == null)
                        player.CurrentItem = player.AddItem(_ballItemType);

            player.ClearBroadcasts();
#if EXILED
            player.Broadcast(1, text);
#else
            player.SendBroadcast(text, 1);
#endif
        }
    }

    protected override void OnFinished()
    {
        var totalTime = TimeSpan.FromSeconds(Config.TotalTimeInSeconds) - _roundTime;
        var time = $"{totalTime.Minutes:00}:{totalTime.Seconds:00}";

#if EXILED
        var classDCount = Player.List.Count(r => r.Role.Type == RoleTypeId.ClassD);
        var sciCount = Player.List.Count(r => r.Role.Type == RoleTypeId.Scientist);
#else
        var classDCount = Player.ReadyList.Count(r => r.Role == RoleTypeId.ClassD);
        var sciCount = Player.ReadyList.Count(r => r.Role == RoleTypeId.Scientist);
#endif
        var text = string.Empty;

        if (classDCount < 1 && sciCount < 1)
            text = Translation.AllDied.Replace("{name}", Name).Replace("{time}", time);
        else if (classDCount < 1)
            text = Translation.ScientistWin.Replace("{name}", Name).Replace("{time}", time);
        else if (sciCount < 1)
            text = Translation.ClassDWin.Replace("{name}", Name).Replace("{time}", time);
        else if (_roundTime.TotalSeconds <= 0) text = Translation.Draw.Replace("{name}", Name).Replace("{time}", time);

        Extensions.Broadcast(text, 10);
    }
}
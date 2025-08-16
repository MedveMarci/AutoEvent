using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using InventorySystem;
using InventorySystem.Items.MarshmallowMan;
using MEC;
using PlayerRoles;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Infection;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private int _overtime = 30;
    internal List<GameObject> SpawnList;
    public override string Name { get; set; } = "Zombie Infection";
    public override string Description { get; set; } = "Zombie mode, the purpose of which is to infect all players";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "zombie";
    private EventHandler _eventHandler { get; set; }
    public bool IsChristmasUpdate { get; set; }
    public bool IsHalloweenUpdate { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Zombie",
        Position = new Vector3(0, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Zombie_Run.ogg",
        Volume = 15
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Player.Hurting += _eventHandler.OnHurting;
        Player.Joined += _eventHandler.OnJoined;
        Player.Died += _eventHandler.OnDied;
#else
        PlayerEvents.Hurting += _eventHandler.OnHurting;
        PlayerEvents.Joined += _eventHandler.OnJoined;
        PlayerEvents.Death += _eventHandler.OnDied;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Player.Hurting -= _eventHandler.OnHurting;
        Player.Joined -= _eventHandler.OnJoined;
        Player.Died -= _eventHandler.OnDied;
#else
        PlayerEvents.Hurting -= _eventHandler.OnHurting;
        PlayerEvents.Joined -= _eventHandler.OnJoined;
        PlayerEvents.Death -= _eventHandler.OnDied;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        _overtime = 30;
        // Halloween update -> check that the marshmallow item exists and not null
        if (Enum.TryParse("Marshmallow", out ItemType marshmallowItemType))
        {
            InventoryItemLoader.AvailableItems.TryGetValue(marshmallowItemType, out var itemBase);
            if (itemBase != null)
            {
                IsHalloweenUpdate = true;
                ForceEnableFriendlyFire = FriendlyFireSettings.Enable;
            }
        }
        // Christmas update -> check that the role exists and it can be obtained
        else if (Enum.TryParse("ZombieFlamingo", out RoleTypeId flamingoRoleType))
        {
            if (PlayerRoleLoader.AllRoles.Keys.Contains(flamingoRoleType)) IsChristmasUpdate = true;
        }

        SpawnList = MapInfo.Map.AttachedBlocks.Where(r => r.name == "Spawnpoint").ToList();
        foreach (var player in Exiled.API.Features.Player.List)
        {
            if (IsChristmasUpdate && Enum.TryParse("Flamingo", out RoleTypeId roleTypeId))
            {
#if EXILED
                player.Role.Set(roleTypeId, RoleSpawnFlags.None);
#else
                player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
#endif
            }
            else
            {
                player.GiveLoadout(Config.PlayerLoadouts);
            }

            player.Position = SpawnList.RandomItem().transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (float time = 15; time > 0; time--)
        {
            Extensions.Broadcast(Translation.Start.Replace("{name}", Name).Replace("{time}", time.ToString("00")), 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        var player = Exiled.API.Features.Player.List.ToList().RandomItem();

        if (IsHalloweenUpdate)
        {
#if EXILED
            player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
#else
            player.SetRole(RoleTypeId.Scientist, flags: RoleSpawnFlags.None);
#endif
            player.EnableEffect<MarshmallowEffect>();
            player.IsGodModeEnabled = true;
        }
        else if (IsChristmasUpdate && Enum.TryParse("ZombieFlamingo", out RoleTypeId roleTypeId))
        {
#if EXILED
            player.Role.Set(roleTypeId, RoleSpawnFlags.None);
#else
            player.SetRole(roleTypeId, flags: RoleSpawnFlags.None);
#endif
        }
        else
        {
            player.GiveLoadout(Config.ZombieLoadouts);
        }

        Extensions.PlayPlayerAudio(SoundInfo.AudioPlayer, player, Config.ZombieScreams.RandomItem(), 15);
    }

    protected override bool IsRoundDone()
    {
        var roleType = RoleTypeId.ClassD;
        if (IsChristmasUpdate && Enum.TryParse("Flamingo", out roleType))
        {
            //nothing
        }

#if EXILED
        if (Exiled.API.Features.Player.List.Count(r => r.Role.Type == roleType) > 0 && _overtime > 0)
#else
        if (Player.ReadyList.Count(r => r.Role == roleType) > 0 && _overtime > 0)
#endif
            return false;

        return true;
    }

    protected override void ProcessFrame()
    {
        var roleType = RoleTypeId.ClassD;
        var time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";

        if (IsChristmasUpdate && Enum.TryParse("Flamingo", out roleType))
        {
            //nothing
        }

        var count = Exiled.API.Features.Player.List.Count(r => r.Role == roleType);
        if (count > 1)
        {
            Extensions.Broadcast(
                Translation.Cycle.Replace("{name}", Name).Replace("{count}", count.ToString()).Replace("{time}", time),
                1);
        }
        else if (count == 1)
        {
            _overtime--;
            Extensions.Broadcast(Translation.ExtraTime
                .Replace("{extratime}", _overtime.ToString("00"))
                .Replace("{time}", $"{time}"), 1);
        }
    }

    protected override void OnFinished()
    {
        var roleType = RoleTypeId.ClassD;
        if (IsChristmasUpdate && Enum.TryParse("Flamingo", out roleType))
        {
            //nothing
        }

        if (Exiled.API.Features.Player.List.Count(r => r.Role == roleType) == 0)
            Extensions.Broadcast(Translation.Win.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"),
                10);
        else
            Extensions.Broadcast(Translation.Lose.Replace("{time}", $"{EventTime.Minutes:00}:{EventTime.Seconds:00}"),
                10);
    }
}
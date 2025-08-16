using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using MEC;
using PlayerRoles;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;
using Random = UnityEngine.Random;
#if EXILED
using Exiled.API.Features;

#else
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Airstrike;

public class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    private CoroutineHandle _grenadeCoroutineHandle;
    public List<GameObject> SpawnList;
    public override string Name { get; set; } = "Airstrike Party";
    public override string Description { get; set; } = "Survive as aistrikes rain down from above.";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "airstrike";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;
    protected override FriendlyFireSettings ForceEnableFriendlyFireAutoban { get; set; } = FriendlyFireSettings.Disable;
    private EventHandler _eventHandler { get; set; }
    public int Stage { get; private set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "DeathParty",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "DeathParty.ogg",
        Volume = 5
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Player.Dying += _eventHandler.OnDying;
        Player.ThrownProjectile += _eventHandler.OnThrownProjectile;
        Player.Hurting += _eventHandler.OnHurting;
#else
        PlayerEvents.Dying += _eventHandler.OnPlayerDying;
        PlayerEvents.ThrewProjectile += _eventHandler.OnPlayerThrewProjectile;
        PlayerEvents.Hurting += _eventHandler.OnPlayerHurting;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Player.Dying -= _eventHandler.OnDying;
        Player.ThrownProjectile -= _eventHandler.OnThrownProjectile;
        Player.Hurting -= _eventHandler.OnHurting;
#else
        PlayerEvents.Dying -= _eventHandler.OnPlayerDying;
        PlayerEvents.ThrewProjectile -= _eventHandler.OnPlayerThrewProjectile;
        PlayerEvents.Hurting -= _eventHandler.OnPlayerHurting;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        Server.FriendlyFire = true;

        SpawnList = MapInfo.Map.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList();
#if EXILED
        foreach (var player in Exiled.API.Features.Player.List)
#else
        foreach (var player in Player.ReadyList)
#endif
        {
            player.GiveLoadout(Config.Loadouts);
            player.Position = SpawnList.RandomItem().transform.position;
        }
    }

    protected override void OnStop()
    {
        Timing.CallDelayed(1.2f, () =>
        {
            if (_grenadeCoroutineHandle.IsRunning) Timing.KillCoroutines(_grenadeCoroutineHandle);
        });
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var _time = 10; _time > 0; _time--)
        {
            Extensions.Broadcast($"<size=100><color=red>{_time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        _grenadeCoroutineHandle = Timing.RunCoroutine(GrenadeCoroutine(), "death_grenade");
    }

    protected override void ProcessFrame()
    {
#if EXILED
        var count = Exiled.API.Features.Player.List.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript)
            .ToString();
#else
        var count = Player.ReadyList.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript).ToString();
#endif
        var cycleTime = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
        Extensions.Broadcast(Translation.Cycle.Replace("{count}", count).Replace("{time}", cycleTime), 1);
    }

    protected override bool IsRoundDone()
    {
#if EXILED
        var playerCount = Exiled.API.Features.Player.List.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#else
        var playerCount = Player.ReadyList.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#endif
        return !(playerCount > (Config.LastPlayerAliveWins ? 1 : 0)
                 && Stage <= Config.Rounds);
    }

    public IEnumerator<float> GrenadeCoroutine()
    {
        Stage = 1;
        var fuse = 10f;
        var height = 20f;
        float count = 20;
        var timing = 1f;
        float scale = 4;
        var radius = MapInfo.Map.AttachedBlocks.First(x => x.name == "Arena").transform.localScale.x / 2 - 6f;
#if EXILED
        while (Exiled.API.Features.Player.List.Count(r => r.IsAlive) > (Config.LastPlayerAliveWins ? 1 : 0) &&
               Stage <= Config.Rounds)
#else
        while (Player.ReadyList.Count(r => r.IsAlive) > (Config.LastPlayerAliveWins ? 1 : 0) && Stage <= Config.Rounds)
#endif
        {
            if (KillLoop) yield break;

            DebugLogger.LogDebug(
                $"Stage: {Stage}/{Config.Rounds}. Radius: {radius}, Scale: {scale}, Count: {count}, Timing: {timing}, Height: {height}, Fuse: {fuse}, Target: {Config.TargetPlayers}");

            // Not the last round.
            if (Stage != Config.Rounds)
            {
                var playerIndex = 0;
                for (var i = 0; i < count; i++)
                {
                    var pos = MapInfo.Map.Position + new Vector3(Random.Range(-radius, radius), height,
                        Random.Range(-radius, radius));
                    // has to be re-iterated every run because a player could have been killed from the last one.
                    if (Config.TargetPlayers)
                        try
                        {
#if EXILED
                            var randomPlayer =
                                Exiled.API.Features.Player.List.Where(x => x.Role == RoleTypeId.ClassD).ToList()
                                    .RandomItem();
#else
                            var randomPlayer = Player.ReadyList.Where(x => x.Role == RoleTypeId.ClassD).ToList()
                                .RandomItem();
#endif
                            pos = randomPlayer.Position;
                            pos.y = height + MapInfo.Map.Position.y;
                        }
                        catch (Exception e)
                        {
                            DebugLogger.LogDebug("Caught an error while targeting a player.", LogLevel.Warn, true);
                            DebugLogger.LogDebug($"{e}");
                        }

                    Extensions.GrenadeSpawn(pos, scale, fuse);
                    yield return Timing.WaitForSeconds(timing);
                    playerIndex++;
                }
            }
            else // last round.
            {
                var pos = MapInfo.Map.Position + new Vector3(Random.Range(-10, 10), 20, Random.Range(-10, 10));
                Extensions.GrenadeSpawn(pos, 75, 10, 0);
            }

            yield return Timing.WaitForSeconds(15f);
            Stage++;

            // Defaults: 
            count += 30; //20,  50,  80,  110, [ignored last round] 1
            timing -= 0.3f; //1.0, 0.7, 0.4, 0.1, [ignored last round] 10
            height -= 5f; //20,  15,  10,  5,   [ignored last round] 20
            fuse -= 2f; //10,  8,   6,   4,   [ignored last round] 10
            scale -= 1; //4,   3,   2,   1,   [ignored last round] 75
            radius += 7f; //4,   11,  18,  25   [ignored last round] 10
        }

        DebugLogger.LogDebug("Finished Grenade Coroutine.");
    }

    protected override void OnFinished()
    {
        if (_grenadeCoroutineHandle.IsRunning)
        {
            KillLoop = true;
            Timing.CallDelayed(1.2f, () =>
            {
                if (_grenadeCoroutineHandle.IsRunning) Timing.KillCoroutines(_grenadeCoroutineHandle);
            });
        }

        var time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
#if EXILED
        var count = Exiled.API.Features.Player.List.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#else
        var count = Player.ReadyList.Count(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#endif
        if (count > 1)
        {
#if EXILED
            Extensions.Broadcast(
                Translation.MorePlayer
                    .Replace("{count}",
                        $"{Exiled.API.Features.Player.List.Count(r => r.Role != RoleTypeId.ChaosConscript)}")
                    .Replace("{time}", time), 10);
#else
            Extensions.Broadcast(
                Translation.MorePlayer
                    .Replace("{count}", $"{Player.ReadyList.Count(r => r.Role != RoleTypeId.ChaosConscript)}")
                    .Replace("{time}", time), 10);
#endif
        }
        else if (count == 1)
        {
#if EXILED
            var player = Exiled.API.Features.Player.List.First(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#else
            var player = Player.ReadyList.First(r => r.IsAlive && r.Role != RoleTypeId.ChaosConscript);
#endif
            player.Health = 1000;
            Extensions.Broadcast(Translation.OnePlayer.Replace("{winner}", player.Nickname).Replace("{time}", time),
                10);
        }
        else
        {
            Extensions.Broadcast(Translation.AllDie.Replace("{time}", time), 10);
        }
    }
}
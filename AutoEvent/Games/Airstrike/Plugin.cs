using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Games.Airstrike.Configs;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;
using Random = UnityEngine.Random;


namespace AutoEvent.Games.Airstrike;

public abstract class Plugin : Event<Configs.Config, Translation>, IEventMap, IEventSound
{
    private CoroutineHandle _grenadeCoroutineHandle;
    public List<GameObject> SpawnList;
    public override string Name { get; set; } = "Airstrike Party";
    public override string Description { get; set; } = "Survive as aistrikes rain down from above.";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "airstrike";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;
    protected override FriendlyFireSettings ForceEnableFriendlyFireAutoban { get; set; } = FriendlyFireSettings.Disable;
    private EventHandler EventHandler { get; set; }
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
        EventHandler = new EventHandler(this);
        PlayerEvents.Dying += EventHandler.OnPlayerDying;
        PlayerEvents.ThrewProjectile += EventHandler.OnPlayerThrewProjectile;
        PlayerEvents.Hurting += EventHandler.OnPlayerHurting;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Dying -= EventHandler.OnPlayerDying;
        PlayerEvents.ThrewProjectile -= EventHandler.OnPlayerThrewProjectile;
        PlayerEvents.Hurting -= EventHandler.OnPlayerHurting;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        Server.FriendlyFire = true;

        SpawnList = MapInfo.Map.AttachedBlocks.Where(x => x.name == "Spawnpoint").ToList();
        foreach (var player in Player.ReadyList)
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
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast($"<size=100><color=red>{time}</color></size>", 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        _grenadeCoroutineHandle = Timing.RunCoroutine(GrenadeCoroutine(), "death_grenade");
    }

    protected override void ProcessFrame()
    {
        var count = Player.ReadyList.Count(r => r.IsAlive).ToString();
        var cycleTime = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
        Extensions.ServerBroadcast(Translation.Cycle.Replace("{count}", count).Replace("{time}", cycleTime), 1);
    }

    protected override bool IsRoundDone()
    {
        var playerCount = Player.ReadyList.Count(r => r.IsAlive);
        return !(playerCount > (Config.LastPlayerAliveWins ? 1 : 0)
                 && Stage <= Config.Rounds);
    }

    private IEnumerator<float> GrenadeCoroutine()
    {
        Stage = 1;
        var fuse = 10f;
        var height = 20f;
        float count = 20;
        var timing = 1f;
        float scale = 4;
        var radius = MapInfo.Map.AttachedBlocks.First(x => x.name == "Arena").transform.localScale.x / 2 - 6f;
        while (Player.ReadyList.Count(r => r.IsAlive) > (Config.LastPlayerAliveWins ? 1 : 0) && Stage <= Config.Rounds)
        {
            if (KillLoop) yield break;

            LogManager.Debug(
                $"Stage: {Stage}/{Config.Rounds}. Radius: {radius}, Scale: {scale}, Count: {count}, Timing: {timing}, Height: {height}, Fuse: {fuse}, Target: {Config.TargetPlayers}");

            // Not the last round.
            if (Stage != Config.Rounds)
            {
                for (var i = 0; i < count; i++)
                {
                    var pos = MapInfo.Map.Position + new Vector3(Random.Range(-radius, radius), height,
                        Random.Range(-radius, radius));
                    // has to be re-iterated every run because a player could have been killed from the last one.
                    if (Config.TargetPlayers)
                        try
                        {
                            var randomPlayer = Player.ReadyList.Where(x => x.Role == RoleTypeId.ClassD).ToList()
                                .RandomItem();
                            pos = randomPlayer.Position;
                            pos.y = height + MapInfo.Map.Position.y;
                        }
                        catch (Exception e)
                        {
                            LogManager.Error($"Caught an error while targeting a player.\n{e}");
                        }

                    Extensions.GrenadeSpawn(pos, scale, fuse, scale);
                    yield return Timing.WaitForSeconds(timing);
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
            scale -= 1; //4, 3, 2, 1,   [ignored last round] 75
            radius += 7f; //4,   11,  18,  25   [ignored last round] 10
        }

        LogManager.Debug("Finished Grenade Coroutine.");
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
        var count = Player.ReadyList.Count(r => r.IsAlive);
        switch (count)
        {
            case > 1:
                Extensions.ServerBroadcast(
                    Translation.MorePlayer
                        .Replace("{count}", $"{Player.ReadyList.Count()}")
                        .Replace("{time}", time), 10);
                break;
            case 1:
            {
                var player = Player.ReadyList.First(r => r.IsAlive);

                player.Health = 1000;
                Extensions.ServerBroadcast(
                    Translation.OnePlayer.Replace("{winner}", player.Nickname).Replace("{time}", time),
                    10);
                break;
            }
            default:
                Extensions.ServerBroadcast(Translation.AllDie.Replace("{time}", time), 10);
                break;
        }
    }
}
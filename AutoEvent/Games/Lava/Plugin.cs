using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Lava;

public abstract class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private GameObject _lava;
    public override string Name { get; set; } = "The floor is LAVA";
    public override string Description { get; set; } = "Survival, in which you need to avoid lava and shoot at others";
    public override string Author { get; set; } = "RisottoMan";
    public override string CommandName { get; set; } = "lava";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Lava",
        Position = new Vector3(0f, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Lava.ogg",
        Volume = 8,
        Loop = false
    };

    protected override void RegisterEvents()
    {
        PlayerEvents.Hurting += EventHandler.OnHurting;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Hurting -= EventHandler.OnHurting;
    }

    protected override void OnStart()
    {
        var spawnpoints = new List<GameObject>();

        foreach (var obj in MapInfo.Map.AttachedBlocks)
            switch (obj.name)
            {
                case "Spawnpoint": spawnpoints.Add(obj); break;
                case "LavaObject": _lava = obj; break;
            }

        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(Config.Loadouts, LoadoutFlags.IgnoreGodMode);
            player.Position = spawnpoints.RandomItem().transform.position;
        }
    }

    protected override IEnumerator<float> BroadcastStartCountdown()
    {
        for (var time = 10; time > 0; time--)
        {
            Extensions.ServerBroadcast(Translation.Start.Replace("{time}", $"{time}"), 1);
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void CountdownFinished()
    {
        _lava.AddComponent<LavaComponent>().StartComponent(this);
        foreach (var player in Player.ReadyList)
            player.GiveInfiniteAmmo(AmmoMode.InfiniteAmmo);
    }

    protected override bool IsRoundDone()
    {
        return !(Player.ReadyList.Count(r => r.IsAlive) > 1 && EventTime.TotalSeconds < 600);
    }

    protected override void ProcessFrame()
    {
        var text = EventTime.TotalSeconds % 2 == 0
            ? "<size=90><color=red><b>《 ! 》</b></color></size>\n"
            : "<size=90><color=red><b>!</b></color></size>\n";

        Extensions.ServerBroadcast(
            text + Translation.Cycle.Replace("{count}", $"{Player.ReadyList.Count(r => r.IsAlive)}"), 1);
        _lava.transform.position += new Vector3(0, 0.08f, 0);
    }

    protected override void OnFinished()
    {
        Extensions.ServerBroadcast(
            Player.ReadyList.Count(r => r.IsAlive) == 1
                ? Translation.Win.Replace("{winner}", Player.ReadyList.First(r => r.IsAlive).Nickname)
                : Translation.AllDead,
            10);
    }
}
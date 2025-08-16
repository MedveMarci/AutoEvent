using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using MEC;
using UnityEngine;
using Player = Exiled.Events.Handlers.Player;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Deathmatch;

public class Plugin : Event<Config, Translation>, IEventMap, IEventSound
{
    private int _needKills;
    public override string Name { get; set; } = "Team Death-Match";
    public override string Description { get; set; } = "Team Death-Match on the Shipment map from MW19";
    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";
    public override string CommandName { get; set; } = "tdm";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;
    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll;
    private EventHandler _eventHandler { get; set; }
    internal int MtfKills { get; set; }
    internal int ChaosKills { get; set; }

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "Shipment",
        Position = new Vector3(0, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "ClassicMusic.ogg",
        Volume = 5
    };

    protected override void RegisterEvents()
    {
        _eventHandler = new EventHandler(this);
#if EXILED
        Player.Joined += _eventHandler.OnJoined;
        Player.Dying += _eventHandler.OnDying;
#else
        PlayerEvents.Joined += _eventHandler.OnJoined;
        PlayerEvents.Dying += _eventHandler.OnDying;
#endif
    }

    protected override void UnregisterEvents()
    {
#if EXILED
        Player.Joined -= _eventHandler.OnJoined;
        Player.Dying -= _eventHandler.OnDying;
#else
        PlayerEvents.Joined -= _eventHandler.OnJoined;
        PlayerEvents.Dying -= _eventHandler.OnDying;
#endif
        _eventHandler = null;
    }

    protected override void OnStart()
    {
        MtfKills = 0;
        ChaosKills = 0;
#if EXILED
        _needKills = Config.KillsPerPerson * Exiled.API.Features.Player.List.Count;
#else
        _needKills = Config.KillsPerPerson * Player.ReadyList.Count();
#endif

        float scale = 1;
        switch (Exiled.API.Features.Player.List.Count())
        {
            case int n when n > 20 && n <= 25: scale = 1.1f; break;
            case int n when n > 25 && n <= 30: scale = 1.2f; break;
            case int n when n > 35: scale = 1.3f; break;
        }

        var count = 0;
        foreach (var player in Exiled.API.Features.Player.List)
        {
            if (count % 2 == 0)
            {
                player.GiveLoadout(Config.NTFLoadouts,
                    LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
                player.Position = RandomClass.GetRandomPosition(MapInfo.Map);
            }
            else
            {
                player.GiveLoadout(Config.ChaosLoadouts,
                    LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
                player.Position = RandomClass.GetRandomPosition(MapInfo.Map);
            }

            count++;
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
        foreach (var player in Exiled.API.Features.Player.List)
            if (player.CurrentItem == null)
                player.CurrentItem = player.AddItem(Config.AvailableWeapons.RandomItem());
    }

    protected override bool IsRoundDone()
    {
        return !(MtfKills < _needKills && ChaosKills < _needKills &&
                 Exiled.API.Features.Player.List.Count(r => r.IsNTF) > 0 &&
#if EXILED
                 Exiled.API.Features.Player.List.Count(r => r.IsCHI) > 0);
#else
                 Player.List.Count(r => r.IsChaos) > 0);
#endif
    }

    protected override void ProcessFrame()
    {
        var mtfColor = "<color=#42AAFF>";
        var chaosColor = "<color=green>";
        var whiteColor = "<color=white>";
        var mtfIndex = mtfColor.Length + (int)((float)MtfKills / _needKills * 20f);
        var chaosIndex = whiteColor.Length + 20 - (int)((float)ChaosKills / _needKills * 20f);
        var mtfString = $"{mtfColor}||||||||||||||||||||{mtfColor}".Insert(mtfIndex, whiteColor);
        var chaosString = $"{whiteColor}||||||||||||||||||||".Insert(chaosIndex, chaosColor);

        Extensions.Broadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{mtftext}", $"{MtfKills} {mtfString}")
                .Replace("{chaostext}", $"{chaosString} {ChaosKills}"), 1);
    }

    protected override void OnFinished()
    {
        if (MtfKills == _needKills)
            Extensions.Broadcast(Translation.MtfWin.Replace("{name}", Name), 10);
        else
            Extensions.Broadcast(Translation.ChaosWin.Replace("{name}", Name), 10);
    }
}
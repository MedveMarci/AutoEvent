using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

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
    private EventHandler EventHandler { get; set; }
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
        EventHandler = new EventHandler(this);
        PlayerEvents.Joined += EventHandler.OnJoined;
        PlayerEvents.Dying += EventHandler.OnDying;
        PlayerEvents.PlacingBlood += EventHandler.OnPlacingBlood;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Joined -= EventHandler.OnJoined;
        PlayerEvents.Dying -= EventHandler.OnDying;
        PlayerEvents.PlacingBlood -= EventHandler.OnPlacingBlood;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        MtfKills = 0;
        ChaosKills = 0;
        _needKills = Config.KillsPerPerson * Player.ReadyList.Count();

        var count = 0;
        foreach (var player in Player.ReadyList)
        {
            player.GiveLoadout(count % 2 == 0 ? Config.NtfLoadouts : Config.ChaosLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);

            player.Position = RandomClass.GetRandomPosition(MapInfo.Map);

            count++;
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
        return !(MtfKills < _needKills && ChaosKills < _needKills &&
                 Player.ReadyList.Count(r => r.IsNTF) > 0 &&
                 Player.ReadyList.Count(r => r.IsChaos) > 0);
    }

    protected override void ProcessFrame()
    {
        const string mtfColor = "<color=#42AAFF>";
        const string chaosColor = "<color=green>";
        const string whiteColor = "<color=white>";
        var mtfIndex = mtfColor.Length + (int)((float)MtfKills / _needKills * 20f);
        var chaosIndex = whiteColor.Length + 20 - (int)((float)ChaosKills / _needKills * 20f);
        var mtfString = $"{mtfColor}||||||||||||||||||||{mtfColor}".Insert(mtfIndex, whiteColor);
        var chaosString = $"{whiteColor}||||||||||||||||||||".Insert(chaosIndex, chaosColor);

        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{mtftext}", $"{MtfKills} {mtfString}")
                .Replace("{chaostext}", $"{chaosString} {ChaosKills}"), 1);
    }

    protected override void OnFinished()
    {
        Extensions.ServerBroadcast(
            MtfKills == _needKills
                ? Translation.MtfWin.Replace("{name}", Name)
                : Translation.ChaosWin.Replace("{name}", Name), 10);
    }
}
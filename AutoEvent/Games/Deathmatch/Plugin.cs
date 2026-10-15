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

    public override string Description { get; set; } = "Team Death-Match between MTF and Chaos Insurgency";

    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";

    public override string CommandName { get; set; } = "tdm";

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll | EventFlags.IgnoreHandcuffing | EventFlags.IgnoreBulletHole | EventFlags.IgnoreBloodDecal;

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
        SoundName = "ClassicMusic.ogg"
    };

    protected override void RegisterEvents()
    {
        EventHandler = new EventHandler(this);
        PlayerEvents.Joined += EventHandler.OnJoined;
        PlayerEvents.Dying += EventHandler.OnDying;
        PlayerEvents.PlacingBlood += EventHandler.OnPlacingBlood;
        PlayerEvents.Cuffing += EventHandler.OnCuffing;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Joined -= EventHandler.OnJoined;
        PlayerEvents.Dying -= EventHandler.OnDying;
        PlayerEvents.PlacingBlood -= EventHandler.OnPlacingBlood;
        PlayerEvents.Cuffing -= EventHandler.OnCuffing;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        MtfKills = 0;
        ChaosKills = 0;
        _needKills = Mathf.Max(1, Config.KillsPerPerson * Player.ReadyList.Count());

        int count = 0;
        foreach (Player player in Player.ReadyList)
        {
            player.GiveLoadout(count % 2 == 0 ? Config.NtfLoadouts : Config.ChaosLoadouts, LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);

            player.Position = RandomClass.GetRandomPosition(MapInfo.Map);

            count++;
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
        return !(MtfKills < _needKills && ChaosKills < _needKills && Player.ReadyList.Count(r => r.IsNTF) > 0 && Player.ReadyList.Count(r => r.IsChaos) > 0);
    }

    protected override void ProcessFrame()
    {
        const string mtfColor = "<color=#42AAFF>";
        const string chaosColor = "<color=green>";
        const string whiteColor = "<color=white>";
        int mtfIndex = mtfColor.Length + (int)(Mathf.Clamp01((float)MtfKills / _needKills) * 20f);
        int chaosIndex = whiteColor.Length + 20 - (int)(Mathf.Clamp01((float)ChaosKills / _needKills) * 20f);
        string mtfString = $"{mtfColor}||||||||||||||||||||{mtfColor}".Insert(mtfIndex, whiteColor);
        string chaosString = $"{whiteColor}||||||||||||||||||||".Insert(chaosIndex, chaosColor);

        Extensions.ServerBroadcast(Translation.Cycle.Replace("{name}", Name).Replace("{mtftext}", $"{MtfKills} {mtfString}").Replace("{chaostext}", $"{chaosString} {ChaosKills}"), 1);
    }

    protected override void OnFinished()
    {
        // The round can also end because a whole team left, not only by reaching the kill goal.
        bool mtfWon = MtfKills >= _needKills || (ChaosKills < _needKills && !Player.ReadyList.Any(r => r.IsChaos));
        Extensions.ServerBroadcast(mtfWon ? Translation.MtfWin.Replace("{name}", Name) : Translation.ChaosWin.Replace("{name}", Name), 10);
    }
}
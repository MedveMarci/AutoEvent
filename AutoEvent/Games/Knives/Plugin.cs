using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace AutoEvent.Games.Knives;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    public override string Name { get; set; } = "Knives of Death";
    public override string Description { get; set; } = "Knife players against each other on a 35hp map from cs 1.6";
    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";
    public override string CommandName { get; set; } = "knives";
    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Disable;

    public MapInfo MapInfo { get; set; } = new()
    {
        MapName = "35hp_2",
        Position = new Vector3(0, 40f, 0f)
    };

    public SoundInfo SoundInfo { get; set; } = new()
    {
        SoundName = "Knife.ogg",
        Volume = 10
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
        var count = 0;
        var spawnList = MapInfo.Map.AttachedBlocks.Where(r => r.name.Contains("Spawnpoint")).ToList();
        foreach (var player in Player.ReadyList)
        {
            if (count % 2 == 0)
            {
                player.GiveLoadout(Config.Team1Loadouts, LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
                player.Position = spawnList.ElementAt(0).transform.position;
            }
            else
            {
                player.GiveLoadout(Config.Team2Loadouts, LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
                player.Position = spawnList.ElementAt(1).transform.position;
            }

            count++;

            player.CurrentItem ??= player.AddItem(ItemType.Jailbird);
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
        foreach (var wall in MapInfo.Map.AttachedBlocks.Where(x => x.name == "Wall")) Object.Destroy(wall);
    }

    protected override bool IsRoundDone()
    {
        return !(Player.ReadyList.Count(r => r.RoleBase.Team == Team.FoundationForces) > 0 &&
                 Player.ReadyList.Count(r => r.RoleBase.Team == Team.ChaosInsurgency) > 0);
    }

    protected override void ProcessFrame()
    {
        var mtfCount = Player.ReadyList.Count(r => r.RoleBase.Team == Team.FoundationForces).ToString();
        var chaosCount = Player.ReadyList.Count(r => r.RoleBase.Team == Team.ChaosInsurgency).ToString();

        Extensions.ServerBroadcast(
            Translation.Cycle.Replace("{name}", Name).Replace("{mtfcount}", mtfCount)
                .Replace("{chaoscount}", chaosCount), 1);
    }

    protected override void OnFinished()
    {
        if (Player.ReadyList.Count(r => r.RoleBase.Team == Team.FoundationForces) == 0)
            Extensions.ServerBroadcast(Translation.ChaosWin.Replace("{name}", Name), 10);
        else if (Player.ReadyList.Count(r => r.RoleBase.Team == Team.ChaosInsurgency) == 0)
            Extensions.ServerBroadcast(Translation.MtfWin.Replace("{name}", Name), 10);
    }
}
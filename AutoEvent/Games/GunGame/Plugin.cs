using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace AutoEvent.Games.GunGame;

public class Plugin : Event<Config, Translation>, IEventSound, IEventMap
{
    private Player _winner;

    public override string Name { get; set; } = "Gun Game";

    public override string Description { get; set; } = "Cool GunGame mode where you get new guns by killing people";

    public override string Author { get; set; } = "RisottoMan/code & xleb.ik/map";

    public override string CommandName { get; set; } = "gungame";

    protected override FriendlyFireSettings ForceEnableFriendlyFire { get; set; } = FriendlyFireSettings.Enable;

    public override EventFlags EventHandlerSettings { get; set; } = EventFlags.IgnoreRagdoll | EventFlags.IgnoreHandcuffing | EventFlags.IgnoreBulletHole | EventFlags.IgnoreBloodDecal;

    private EventHandler EventHandler { get; set; }

    internal List<Vector3> SpawnPoints { get; private set; }

    internal Dictionary<Player, Stats> PlayerStats { get; set; }

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
        PlayerStats = new Dictionary<Player, Stats>();

        EventHandler = new EventHandler(this);
        PlayerEvents.Dying += EventHandler.OnDying;
        PlayerEvents.Joined += EventHandler.OnJoined;
    }

    protected override void UnregisterEvents()
    {
        PlayerEvents.Dying -= EventHandler.OnDying;
        PlayerEvents.Joined -= EventHandler.OnJoined;
        EventHandler = null;
    }

    protected override void OnStart()
    {
        _winner = null;
        SpawnPoints = [];

        foreach (GameObject point in MapInfo.Map.AttachedBlocks.Where(x => x.name == "Spawnpoint"))
            SpawnPoints.Add(point.transform.position);

        foreach (Player player in Player.ReadyList)
        {
            if (!PlayerStats.ContainsKey(player)) PlayerStats.Add(player, new Stats(0));

            player.ClearInventory();
            player.GiveLoadout(Config.Loadouts, LoadoutFlags.IgnoreWeapons | LoadoutFlags.IgnoreGodMode);
            player.Position = SpawnPoints.RandomItem();
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
        foreach (Player player in Player.ReadyList.Where(r => r.IsAlive)) EventHandler.GetWeaponForPlayer(player);
    }

    protected override bool IsRoundDone()
    {
        // Winner is not null &&
        // Over one player is alive && 
        // Elapsed time is smaller than 10 minutes (+ countdown)
        return !(_winner == null && Player.ReadyList.Count(r => r.IsAlive) > 1 && EventTime.TotalSeconds < 600);
    }

    protected override void ProcessFrame()
    {
        KeyValuePair<Player, Stats> leaderStat = PlayerStats.OrderByDescending(r => r.Value.Kill).FirstOrDefault();
        List<GunRole> gunsInOrder = [.. Config.Guns.OrderByDescending(x => x.KillsRequired)];
        if (leaderStat.Key is null || gunsInOrder.Count == 0)
            return;

        GunRole leadersWeapon = gunsInOrder.FirstOrDefault(x => leaderStat.Value.Kill >= x.KillsRequired);
        int killsToWin = gunsInOrder[0].KillsRequired;
        foreach (Player pl in Player.ReadyList)
        {
            // Players without stats (e.g. joined as spectator) have no kills yet.
            int kills = PlayerStats.TryGetValue(pl, out Stats stats) ? stats.Kill : 0;
            if (stats != null && kills >= killsToWin)
                _winner = pl;

            gunsInOrder.TryGetFirstIndex(x => kills >= x.KillsRequired, out int indexOfFirst);

            string nextGun;
            int killsNeeded;
            if (indexOfFirst <= 0)
            {
                killsNeeded = gunsInOrder[0].KillsRequired + 1 - kills;
                nextGun = "Last Weapon";
            }
            else
            {
                GunRole nextGunRole = gunsInOrder[indexOfFirst - 1];
                nextGun = nextGunRole.Item == ItemType.None ? "Last Weapon" : nextGunRole.Item.ToString();
                killsNeeded = nextGunRole.KillsRequired - kills;
            }

            pl.Broadcast(Translation.Cycle.Replace("{name}", Name).Replace("{gun}", nextGun).Replace("{kills}", $"{killsNeeded}").Replace("{leadnick}", leaderStat.Key.Nickname).Replace("{leadgun}", $"{(leadersWeapon is null ? nextGun : leadersWeapon.Item)}"), 1);
        }
    }

    protected override void OnFinished()
    {
        if (_winner != null)
        {
            string text = Translation.Winner.Replace("{name}", Name).Replace("{winner}", _winner.Nickname);
            Extensions.ServerBroadcast(text, 10);
        }
        else
        {
            // Time ran out or everyone else left: the player with the most kills wins.
            Player leader = PlayerStats.Where(r => r.Key is { IsReady: true, IsAlive: true }).OrderByDescending(r => r.Value.Kill).Select(r => r.Key).FirstOrDefault() ?? Player.ReadyList.FirstOrDefault(r => r.IsAlive);
            if (leader != null)
                Extensions.ServerBroadcast(Translation.Winner.Replace("{name}", Name).Replace("{winner}", leader.Nickname), 10);
        }

        foreach (Player player in Player.ReadyList)
            player.ClearInventory();
    }
}
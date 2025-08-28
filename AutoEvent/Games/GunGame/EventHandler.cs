using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoEvent.Games.GunGame;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
        PlayerStats = plugin.PlayerStats;
    }

    internal Dictionary<Player, Stats> PlayerStats
    {
        get => _plugin.PlayerStats;
        set => _plugin.PlayerStats = value;
    }

    public void OnJoined(PlayerJoinedEventArgs ev)
    {
        if (!PlayerStats.ContainsKey(ev.Player)) PlayerStats.Add(ev.Player, new Stats(0));

        ev.Player.GiveLoadout(_plugin.Config.Loadouts, LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);
        ev.Player.Position = _plugin.SpawnPoints.RandomItem();
        GetWeaponForPlayer(ev.Player);
    }

    public void OnDying(PlayerDyingEventArgs ev)
    {
        ev.IsAllowed = false;

        if (ev.Attacker != null)
        {
            if (!PlayerStats.TryGetValue(ev.Attacker, out var statsAttacker))
            {
                PlayerStats.Add(ev.Attacker, new Stats(1));
                GetWeaponForPlayer(ev.Attacker, true);
            }
            else
            {
                statsAttacker.Kill++;
                GetWeaponForPlayer(ev.Attacker, true);
            }
        }

        if (ev.Player == null) return;
        GetWeaponForPlayer(ev.Player);
        Timing.CallDelayed(Timing.WaitForOneFrame, () => { ev.Player.Position = _plugin.SpawnPoints.RandomItem(); });
    }

    public void GetWeaponForPlayer(Player player, bool isHeal = false)
    {
        if (player is null)
            return;

        if (_plugin.Config.Guns is null)
        {
            var conf = new Config();
            _plugin.Config.Guns = conf.Guns;
        }

        PlayerStats ??= new Dictionary<Player, Stats>();

        if (!PlayerStats.ContainsKey(player)) PlayerStats.Add(player, new Stats(0));

        var itemType = _plugin.Config.Guns.OrderByDescending(y => y.KillsRequired)
            .FirstOrDefault(x => PlayerStats[player].Kill >= x.KillsRequired)!.Item;

        player.EnableEffect<SpawnProtected>(1, .1f);

        player.Heal(500); // Since the player does not die, his hp goes into negative hp, so need to completely heal the player.
        if (player.CurrentItem != null && player.CurrentItem.Type == itemType) return;
        player.ClearItems();
        player.CurrentItem ??= player.AddItem(itemType);
    }

    public static void OnPlacingBlood(PlayerPlacingBloodEventArgs ev)
    {
        ev.IsAllowed = false;
    }
}
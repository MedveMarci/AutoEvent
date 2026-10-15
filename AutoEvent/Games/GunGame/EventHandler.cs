using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.GunGame;

public class EventHandler
{
    private readonly Plugin _plugin;

    internal Dictionary<Player, Stats> PlayerStats
    {
        get => _plugin.PlayerStats;
        set => _plugin.PlayerStats = value;
    }

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
        PlayerStats = plugin.PlayerStats;
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
            if (!PlayerStats.TryGetValue(ev.Attacker, out Stats statsAttacker))
            {
                PlayerStats.Add(ev.Attacker, new Stats(1));
                GetWeaponForPlayer(ev.Attacker);
            }
            else
            {
                statsAttacker.Kill++;
                GetWeaponForPlayer(ev.Attacker);
            }

            if (ev.Attacker.CurrentItem is JailbirdItem jailbirdItem) jailbirdItem.Base.TotalChargesPerformed = 0;
        }

        if (ev.Player == null) return;
        GetWeaponForPlayer(ev.Player);
        Vector3 deathPos = ev.Player.Position;
        Timing.CallDelayed(Timing.WaitForOneFrame, () =>
        {
            List<Vector3> safe = [.. _plugin.SpawnPoints.Where(p => Vector3.Distance(p, deathPos) > 3f)];
            ev.Player.Position = safe.Count > 0 ? safe.RandomItem() : _plugin.SpawnPoints.RandomItem();
        });
    }

    public void GetWeaponForPlayer(Player player)
    {
        if (player is null)
            return;

        if (_plugin.Config.Guns is null)
        {
            Config conf = new();
            _plugin.Config.Guns = conf.Guns;
        }

        PlayerStats ??= new Dictionary<Player, Stats>();

        if (!PlayerStats.ContainsKey(player)) PlayerStats.Add(player, new Stats(0));

        ItemType itemType = _plugin.Config.Guns.OrderByDescending(y => y.KillsRequired).FirstOrDefault(x => PlayerStats[player].Kill >= x.KillsRequired)!.Item;

        if (_plugin.Config.SpawnProtectionDuration > 0)
            player.EnableEffect<SpawnProtected>(1, _plugin.Config.SpawnProtectionDuration);

        player.Heal(500); // Since the player does not die, his hp goes into negative hp, so need to completely heal the player.
        if (player.CurrentItem != null && player.CurrentItem.Type == itemType) return;
        player.ClearItems();
        player.CurrentItem ??= player.AddItem(itemType);
    }
}
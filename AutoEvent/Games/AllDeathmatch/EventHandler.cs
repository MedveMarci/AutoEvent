using AutoEvent.API;
using AutoEvent.API.Enums;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;

namespace AutoEvent.Games.AllDeathmatch;

public class EventHandler(Plugin plugin)
{
    public void OnJoined(PlayerJoinedEventArgs ev)
    {
        if (!plugin.TotalKills.ContainsKey(ev.Player.NetworkId)) plugin.TotalKills.Add(ev.Player.NetworkId, 0);

        SpawnPlayerAfterDeath(ev.Player);
    }

    public void OnLeft(PlayerLeftEventArgs ev)
    {
        plugin.TotalKills.Remove(ev.Player.NetworkId);
    }

    public void OnPlayerDying(PlayerDyingEventArgs ev)
    {
        ev.IsAllowed = false;
        if (ev.Attacker != null && plugin.TotalKills.ContainsKey(ev.Attacker.NetworkId))
            plugin.TotalKills[ev.Attacker.NetworkId]++;
        SpawnPlayerAfterDeath(ev.Player);
    }

    private void SpawnPlayerAfterDeath(Player player)
    {
        player.EnableEffect<Flashed>(duration: .1f);
        player.EnableEffect<SpawnProtected>(duration: 2f);
        player.Health = player.MaxHealth;
        player.ClearItems();
        if (!player.IsAlive)
            player.GiveLoadout(plugin.Config.NtfLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);

        player.CurrentItem ??= player.AddItem(plugin.Config.AvailableWeapons.RandomItem());
        var pos = plugin.SpawnList.RandomItem().transform.position;
        Timing.CallDelayed(0.1f, () => player.Position = pos);
    }

    public static void OnPlacingBlood(PlayerPlacingBloodEventArgs ev)
    {
        ev.IsAllowed = false;
    }

    public static void OnCuffing(PlayerCuffingEventArgs ev)
    {
        ev.IsAllowed = false;
    }
}
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
        if (!plugin.TotalKills.ContainsKey(ev.Player.UserId)) plugin.TotalKills.Add(ev.Player.UserId, 0);

        SpawnPlayerAfterDeath(ev.Player);
    }

    public void OnLeft(PlayerLeftEventArgs ev)
    {
        plugin.TotalKills.Remove(ev.Player.UserId);
    }

    public void OnPlayerDying(PlayerDyingEventArgs ev)
    {
        ev.IsAllowed = false;
        if (ev.Attacker != null)
            plugin.TotalKills[ev.Attacker.UserId]++;
        SpawnPlayerAfterDeath(ev.Player);
    }

    private void SpawnPlayerAfterDeath(Player player)
    {
        player.EnableEffect<Flashed>(duration: .1f);
        player.EnableEffect<SpawnProtected>(duration: 1f);
        player.Heal(player.MaxHealth);
        player.ClearItems();
        if (!player.IsAlive)
            player.GiveLoadout(plugin.Config.NtfLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);

        player.CurrentItem ??= player.AddItem(plugin.Config.AvailableWeapons.RandomItem());
        var pos = plugin.SpawnList.RandomItem().transform.position;
        LogManager.Debug(pos.ToString());
        LogManager.Debug(player.Position.ToString());
        Timing.CallDelayed(0.1f, () => player.Position = pos);
    }
}
using AutoEvent.API.Enums;
using CustomPlayerEffects;
#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.AllDeathmatch;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnJoined(JoinedEventArgs ev)
#else
    public void OnJoined(PlayerJoinedEventArgs ev)
#endif
    {
        if (!_plugin.TotalKills.ContainsKey(ev.Player)) _plugin.TotalKills.Add(ev.Player, 0);

        SpawnPlayerAfterDeath(ev.Player);
    }

#if EXILED
    public void OnLeft(LeftEventArgs ev)
#else
    public void OnLeft(PlayerLeftEventArgs ev)
#endif
    {
        if (_plugin.TotalKills.ContainsKey(ev.Player)) _plugin.TotalKills.Remove(ev.Player);
    }
#if EXILED
    public void OnPlayerDying(DyingEventArgs ev)
#else
    public void OnPlayerDying(PlayerDyingEventArgs ev)
#endif
    {
        ev.IsAllowed = false;
        _plugin.TotalKills[ev.Attacker]++;
        SpawnPlayerAfterDeath(ev.Player);
    }

    private void SpawnPlayerAfterDeath(Player player)
    {
        player.EnableEffect<Flashed>(duration: .1f);
        player.EnableEffect<SpawnProtected>(duration: .1f);
        player.Heal(500); // Since the player does not die, his hp goes into negative hp, so need to completely heal the player.
        player.ClearItems();
        if (!player.IsAlive)
            player.GiveLoadout(_plugin.Config.NTFLoadouts,
                LoadoutFlags.ForceInfiniteAmmo | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons);

        if (player.CurrentItem == null)
            player.CurrentItem = player.AddItem(_plugin.Config.AvailableWeapons.RandomItem());

        player.Position = _plugin.SpawnList.RandomItem().transform.position;
    }
}
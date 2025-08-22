using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace AutoEvent.Games.MusicalChairs;

public class EventHandler(Plugin plugin)
{
    public static void OnHurting(PlayerHurtingEventArgs ev)
    {
        // The players will not die from the explosion
        if (ev.DamageHandler is ExplosionDamageHandler explosionDamageHandler) explosionDamageHandler.Damage = 0;
    }

    public void OnDied(PlayerDeathEventArgs ev)
    {
        // Remove the dead player from the dictionary
        plugin.PlayerDict.Remove(ev.Player);

        // If the player is dead, then remove the last platform
        var playerCount = Player.ReadyList.Count(r => r.IsAlive);
        if (playerCount > 0)
            plugin.Platforms = Functions.RearrangePlatforms(playerCount, plugin.Platforms, plugin.MapInfo.Position);
    }

    public void OnLeft(PlayerLeftEventArgs ev)
    {
        // Remove the left player from the dictionary
        plugin.PlayerDict.Remove(ev.Player);
    }
}
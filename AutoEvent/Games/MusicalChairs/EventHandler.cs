using System.Linq;
#if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using PlayerStatsSystem;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.MusicalChairs;

public class EventHandler
{
    private readonly Plugin _plugin;
    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }
#if EXILED
    public void OnHurting(HurtingEventArgs ev)
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
#endif

    {
        // The players will not die from the explosion
#if EXILED
        if (ev.DamageHandler.Type is DamageType.Explosion) ev.DamageHandler.Damage = 0;
#else
        if (ev.DamageHandler is ExplosionDamageHandler explosionDamageHandler) explosionDamageHandler.Damage = 0;
#endif
    }

#if EXILED
    public void OnDied(DiedEventArgs ev)
#else
    public void OnDied(PlayerDeathEventArgs ev)
#endif
    {
        // Remove the dead player from the dictionary
        if (_plugin.PlayerDict.ContainsKey(ev.Player)) _plugin.PlayerDict.Remove(ev.Player);

        // If the player is dead, then remove the last platform
        var playerCount = Player.List.Count(r => r.IsAlive);
        if (playerCount > 0)
            _plugin.Platforms = Functions.RearrangePlatforms(playerCount, _plugin.Platforms, _plugin.MapInfo.Position);
    }

#if EXILED
    public void OnLeft(LeftEventArgs ev)
#else
    public void OnLeft(PlayerLeftEventArgs ev)
#endif
    {
        // Remove the left player from the dictionary
        if (_plugin.PlayerDict.ContainsKey(ev.Player)) _plugin.PlayerDict.Remove(ev.Player);
    }
}
#if EXILED
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Player;

#else
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
#endif

namespace AutoEvent.Games.Lava;

public class EventHandler
{
    private Plugin _plugin;
    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }
#if EXILED
    public void OnHurting(HurtingEventArgs ev)
    {
        if (ev.DamageHandler.Type is DamageType.Falldown) ev.IsAllowed = false;
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;
#endif

        if (ev.Attacker != null && ev.Player != null)
        {
#if EXILED
            ev.Attacker.ShowHitMarker();
            ev.Amount = 10;
#else
            ev.Attacker.SendHitMarker();
            if (ev.DamageHandler is StandardDamageHandler damageHandler)
                damageHandler.Damage = 10;

#endif
        }
    }
}
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;

namespace AutoEvent.Games.Lava;

public abstract class EventHandler
{
    public static void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;


        if (ev.Attacker == null || ev.Player == null) return;
        ev.Attacker.SendHitMarker();
        if (ev.DamageHandler is StandardDamageHandler damageHandler)
            damageHandler.Damage = 10;
    }
}
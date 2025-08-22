using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;

namespace AutoEvent.Games.Knives;

public abstract class EventHandler
{
    public static void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;
    }
}
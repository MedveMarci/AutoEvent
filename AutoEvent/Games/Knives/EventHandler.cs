#if EXILED
using Exiled.Events.EventArgs.Player;
using DamageType = Exiled.API.Enums.DamageType;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Knives;

public class EventHandler
{
#if EXILED
    public void OnHurting(HurtingEventArgs ev)
    {
        if (ev.DamageHandler.Type == DamageType.Falldown) ev.IsAllowed = false;
    }
#else
    public void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation) ev.IsAllowed = false;
    }
#endif
}
#if EXILED
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp173;

#else
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Arguments.ServerEvents;
#endif

namespace AutoEvent.Games.Escape;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnAnnoucingScpTermination(AnnouncingScpTerminationEventArgs ev)
    {
        ev.IsAllowed = false;
    }
#else
    public void OnAnnoucingScpTermination(CassieAnnouncingEventArgs ev)
    {
        if (ev.Words.Contains("SCP") && ev.Words.Contains("CONTAINED"))
            ev.IsAllowed = false;
    }
#endif

#if EXILED
    public void OnJoined(JoinedEventArgs ev)
#else
    public void OnJoined(PlayerJoinedEventArgs ev)
#endif
    {
        ev.Player.GiveLoadout(_plugin.Config.Scp173Loadout);
    }

#if EXILED
    public void OnPlacingTantrum(PlacingTantrumEventArgs ev)
#else
    public void OnPlacingTantrum(Scp173CreatingTantrumEventArgs ev)
#endif
    {
        ev.IsAllowed = false;
    }

#if EXILED
    public void OnUsingBreakneckSpeeds(UsingBreakneckSpeedsEventArgs ev)
#else
    public void OnUsingBreakneckSpeeds(Scp173BreakneckSpeedChangingEventArgs ev)
#endif
    {
        ev.IsAllowed = false;
    }
}
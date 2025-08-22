using AutoEvent.API;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Arguments.ServerEvents;

namespace AutoEvent.Games.Escape;

public class EventHandler(Plugin plugin)
{
    public static void OnAnnoucingScpTermination(CassieAnnouncingEventArgs ev)
    {
        if (ev.Words.Contains("SCP") && ev.Words.Contains("CONTAINED"))
            ev.IsAllowed = false;
    }

    public void OnJoined(PlayerJoinedEventArgs ev)
    {
        ev.Player.GiveLoadout(plugin.Config.Scp173Loadout);
    }

    public static void OnPlacingTantrum(Scp173CreatingTantrumEventArgs ev)
    {
        ev.IsAllowed = false;
    }

    public static void OnUsingBreakneckSpeeds(Scp173BreakneckSpeedChangingEventArgs ev)
    {
        ev.IsAllowed = false;
    }
}
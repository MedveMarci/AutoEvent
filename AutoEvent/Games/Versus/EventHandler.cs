#if EXILED
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Versus;

public class EventHandler
{
    private readonly Plugin _plugin;
    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }
#if EXILED
    public void OnDying(DyingEventArgs ev)
#else
    public void OnDying(PlayerDyingEventArgs ev)
#endif
    {
        ev.Player.ClearInventory();

        if (ev.Player == _plugin.ClassD) _plugin.ClassD = null;

        if (ev.Player == _plugin.Scientist) _plugin.Scientist = null;
    }
#if EXILED
    public void OnJailbirdCharge(ChargingJailbirdEventArgs ev)
    {
        ev.IsAllowed = _plugin.Config.JailbirdCanCharge;
    }
#else
    public void OnProcessingJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeStarted)
            ev.IsAllowed = _plugin.Config.JailbirdCanCharge;
    }
#endif
}
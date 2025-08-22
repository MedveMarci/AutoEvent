using InventorySystem.Items.Jailbird;
using LabApi.Events.Arguments.PlayerEvents;

namespace AutoEvent.Games.Versus;

public class EventHandler(Plugin plugin)
{
    public void OnDying(PlayerDyingEventArgs ev)

    {
        ev.Player.ClearInventory();

        if (ev.Player == plugin.ClassD) plugin.ClassD = null;

        if (ev.Player == plugin.Scientist) plugin.Scientist = null;
    }

    public void OnProcessingJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeStarted)
            ev.IsAllowed = plugin.Config.JailbirdCanCharge;
    }
}
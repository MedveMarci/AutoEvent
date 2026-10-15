using InventorySystem.Items.Jailbird;
using LabApi.Events.Arguments.PlayerEvents;

namespace AutoEvent.Games.Versus;

public class EventHandler(Plugin plugin)
{
    public void OnDying(PlayerDyingEventArgs ev)
    {
        ev.Player.ClearInventory();

        if (ev.Player == plugin.ClassD)
        {
            plugin.ClassD = null;
            plugin.ClassDLifespan = 0;
            plugin.FinishDuel(plugin.Scientist, true);
        }
        else if (ev.Player == plugin.Scientist)
        {
            plugin.Scientist = null;
            plugin.ScientistLifespan = 0;
            plugin.FinishDuel(plugin.ClassD, false);
        }
    }

    public void OnProcessingJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeLoadTriggered)
            ev.IsAllowed = plugin.Config.JailbirdCanCharge;
    }
}
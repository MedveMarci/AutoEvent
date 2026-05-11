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
            if (plugin.Scientist != null)
            {
                plugin.Scientist.CurrentItem = null;
                plugin.Scientist.RemoveItem(ItemType.Jailbird);
            }

            plugin.ScientistLifespan += 1;
        }
        else if (ev.Player == plugin.Scientist)
        {
            plugin.Scientist = null;
            plugin.ScientistLifespan = 0;
            if (plugin.ClassD != null)
            {
                plugin.ClassD.CurrentItem = null;
                plugin.ClassD.RemoveItem(ItemType.Jailbird);
            }

            plugin.ClassDLifespan += 1;
        }

        if (plugin.Config.JailbirdLifespan == 0) return;
        if (plugin.ScientistLifespan >= plugin.Config.JailbirdLifespan)
            plugin.Scientist?.Kill(plugin.Translation.MaxRoundReached);
        else if (plugin.ClassDLifespan >= plugin.Config.JailbirdLifespan)
            plugin.ClassD?.Kill(plugin.Translation.MaxRoundReached);
    }

    public void OnProcessingJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        if (ev.Message == JailbirdMessageType.ChargeLoadTriggered)
            ev.IsAllowed = plugin.Config.JailbirdCanCharge;
    }
}
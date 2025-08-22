using AutoEvent.API;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using PlayerStatsSystem;

namespace AutoEvent.Games.Airstrike;

public class EventHandler(Plugin plugin)
{
    public void OnPlayerDying(PlayerDyingEventArgs ev)
    {
        if (!plugin.Config.RespawnPlayersWithGrenades)
            return;

        ev.Player.GiveLoadout(plugin.Config.FailureLoadouts);
        ev.Player.Position = plugin.SpawnList.RandomItem().transform.position;
        ev.Player.CurrentItem = ev.Player.AddItem(ItemType.GrenadeHE);
        ev.Player.SendHint("You have a grenade! Throw it at the people who are still alive!", 5f);
        ev.Player.IsGodModeEnabled = true;
    }

    public static void OnPlayerThrewProjectile(PlayerThrewProjectileEventArgs ev)
    {
        Timing.CallDelayed(3f, () =>
        {
            if (AutoEvent.EventManager.CurrentEvent is Plugin)
                ev.Player.CurrentItem = ev.Player.AddItem(ItemType.GrenadeHE);
        });
    }

    public void OnPlayerHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is not ExplosionDamageHandler) return;
        ev.IsAllowed = false;
        ev.Player.Damage(plugin.Stage != 5 ? 10 : 100, "Grenade");
    }
}
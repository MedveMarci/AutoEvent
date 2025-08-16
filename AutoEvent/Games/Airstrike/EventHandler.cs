using MEC;
using PlayerRoles;
using PlayerStatsSystem;
#if EXILED
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Airstrike;

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
    public void OnPlayerDying(PlayerDyingEventArgs ev)
#endif
    {
        if (!_plugin.Config.RespawnPlayersWithGrenades)
            return;

        ev.Player.GiveLoadout(_plugin.Config.FailureLoadouts);
        ev.Player.Position = _plugin.SpawnList.RandomItem().transform.position;
        ev.Player.CurrentItem = ev.Player.AddItem(ItemType.GrenadeHE);
#if EXILED
        ev.Player.ShowHint("You have a grenade! Throw it at the people who are still alive!", 5f);
#else
        ev.Player.SendHint("You have a grenade! Throw it at the people who are still alive!", 5f);
#endif
        ev.Player.IsGodModeEnabled = true;
    }
#if EXILED
    public void OnThrownProjectile(ThrownProjectileEventArgs ev)
#else
    public void OnPlayerThrewProjectile(PlayerThrewProjectileEventArgs ev)
#endif
    {
        if (ev.Player.Role != RoleTypeId.ChaosConscript)
            return;

        Timing.CallDelayed(3f, () => { ev.Player.CurrentItem = ev.Player.AddItem(ItemType.GrenadeHE); });
    }
#if EXILED
    public void OnHurting(HurtingEventArgs ev)
#else
    public void OnPlayerHurting(PlayerHurtingEventArgs ev)
#endif
    {
        if (ev.DamageHandler is ExplosionDamageHandler)
        {
            ev.IsAllowed = false;

            if (_plugin.Stage != 5)
            {
#if EXILED
                ev.Player.Hurt(10, "Grenade");
#else
                ev.Player.Damage(10, "Grenade");
#endif
            }
            else
            {
#if EXILED
                ev.Player.Hurt(100, "Grenade");
#else
                ev.Player.Damage(100, "Grenade");
#endif
            }
        }
    }
}
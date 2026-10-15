using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using UnityEngine;

namespace AutoEvent.Games.Lava;

public class EventHandler
{
    public static void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler.DeathScreenText == DeathTranslations.Falldown.DeathscreenTranslation)
        {
            ev.IsAllowed = false;
            return;
        }

        if (ev.Attacker == null || ev.Player == null) return;
        ev.Attacker.SendHitMarker();
        if (ev.DamageHandler is StandardDamageHandler damageHandler)
            damageHandler.Damage = 10;
    }

    public void OnPickedUpItem(PlayerPickedUpItemEventArgs ev)
    {
        if (ev.Item is not FirearmItem firearm) return;

        if (firearm.MaxAmmo > 0)
            firearm.StoredAmmo = firearm.MaxAmmo;

        if (firearm.ChamberMax > 0)
        {
            firearm.ChamberedAmmo = firearm.ChamberMax;
            firearm.Cocked = true;
        }

        ev.Player.AddAmmo(firearm.AmmoType, (ushort)Mathf.Max(firearm.MaxAmmo, 1));
        ev.Player.CurrentItem = ev.Item;
    }
}
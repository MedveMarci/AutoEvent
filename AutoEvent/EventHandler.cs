using AutoEvent.API;
using AutoEvent.API.Enums;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;

namespace AutoEvent;

internal class EventHandler : CustomEventsHandler
{
    public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRespawnTeam))
            ev.IsAllowed = false;
        base.OnServerWaveRespawning(ev);
    }

    public override void OnServerWaveTeamSelecting(WaveTeamSelectingEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRespawnTeam))
            ev.IsAllowed = false;
        base.OnServerWaveTeamSelecting(ev);
    }

    public override void OnServerLczDecontaminationStarting(LczDecontaminationStartingEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDecontaminating))
            ev.IsAllowed = false;
        base.OnServerLczDecontaminationStarting(ev);
    }

    public override void OnPlayerPlacingBulletHole(PlayerPlacingBulletHoleEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreBulletHole))
            ev.IsAllowed = false;
        base.OnPlayerPlacingBulletHole(ev);
    }

    public override void OnPlayerSpawningRagdoll(PlayerSpawningRagdollEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRagdoll))
            ev.IsAllowed = false;
        base.OnPlayerSpawningRagdoll(ev);
    }

    public override void OnServerPickupCreated(PickupCreatedEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingAmmo) &&
            ev.Pickup.Type.GetName().Contains("Ammo"))
            ev.Pickup.Destroy();
        base.OnServerPickupCreated(ev);
    }

    public override void OnPlayerShootingWeapon(PlayerShootingWeaponEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreInfiniteAmmo))
            return;

        if (!Extensions.InfiniteAmmoList.ContainsKey(ev.Player.UserId))
            return;

        if (ev.FirearmItem.Base.TryGetModule<MagazineModule>(out var module))
        {
            var playersAmmo = module.AmmoMax - module.AmmoStored;
            ev.Player.SetAmmo(module.AmmoType, (ushort)playersAmmo);
        }

        if (ev.FirearmItem.Base.TryGetModule<CylinderAmmoModule>(out var revModule))
        {
            var playersAmmo = revModule.AmmoMax - revModule.AmmoStored;
            ev.Player.SetAmmo(revModule.AmmoType, (ushort)playersAmmo);
        }

        base.OnPlayerShootingWeapon(ev);
    }

    public override void OnPlayerDroppingAmmo(PlayerDroppingAmmoEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingAmmo))
            ev.IsAllowed = false;
        base.OnPlayerDroppingAmmo(ev);
    }

    public override void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)

    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingItem))
            ev.IsAllowed = false;
        base.OnPlayerDroppingItem(ev);
    }

    public override void OnPlayerCuffing(PlayerCuffingEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is not { } activeEvent) return;
        if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreHandcuffing))
            ev.IsAllowed = false;
        base.OnPlayerCuffing(ev);
    }

    public override void OnPlayerDying(PlayerDyingEventArgs ev)
    {
        if (AutoEvent.EventManager.CurrentEvent is null)
            return;

        if (!ev.IsAllowed)
            return;

        Extensions.InfinityStaminaList.Remove(ev.Player.UserId);
        Extensions.InfiniteAmmoList.Remove(ev.Player.UserId);
        base.OnPlayerDying(ev);
    }
}
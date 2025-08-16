using AutoEvent.API.Enums;
using AutoEvent.Interfaces;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Attachments;
#if EXILED
using Exiled.Events.Handlers;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
#else
using InventorySystem.Items.Firearms.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using System.Linq;
#endif

namespace AutoEvent;

internal class EventHandler
{
    private readonly AutoEvent _plugin;

    public EventHandler(AutoEvent plugin)
    {
        _plugin = plugin;
#if EXILED
        Server.RespawningTeam += OnRespawningTeam;
        Server.SelectingRespawnTeam += OnSelectingRespawnTeam;
        Map.Decontaminating += OnDecontaminating;
        Map.PlacingBulletHole += OnPlacingBulletHole;
        Map.PickupAdded += OnPickupAdded;
        Player.SpawningRagdoll += OnSpawningRagdoll;
        Player.Shooting += OnShooting;
        Player.DroppingAmmo += OnDroppingAmmo;
        Player.DroppingItem += OnDroppingItem;
        Player.Handcuffing += OnHandcuffing;
        Player.Dying += OnDying;
#else
        ServerEvents.WaveRespawning += OnWaveRespawning;
        ServerEvents.WaveTeamSelecting += OnWaveTeamSelecting;
        ServerEvents.LczDecontaminationStarting += OnLczDecontaminationStarting;
        PlayerEvents.PlacingBulletHole += OnPlayerPlacingBulletHole;
        ServerEvents.PickupCreated += OnPickupCreated;
        PlayerEvents.SpawningRagdoll += OnPlayerSpawningRagdoll;
        PlayerEvents.ShootingWeapon += OnPlayerShootingWeapon;
        PlayerEvents.DroppingAmmo += OnPlayerDroppingAmmo;
        PlayerEvents.DroppingItem += OnPlayerDroppingItem;
        PlayerEvents.Cuffing += OnPlayerCuffing;
        PlayerEvents.Dying += OnPlayerDying;
#endif
    }

    ~EventHandler()
    {
#if EXILED
        Server.RespawningTeam -= OnRespawningTeam;
        Server.SelectingRespawnTeam -= OnSelectingRespawnTeam;
        Map.Decontaminating -= OnDecontaminating;
        Map.PlacingBulletHole -= OnPlacingBulletHole;
        Map.PickupAdded -= OnPickupAdded;
        Player.SpawningRagdoll -= OnSpawningRagdoll;
        Player.Shooting -= OnShooting;
        Player.DroppingAmmo -= OnDroppingAmmo;
        Player.DroppingItem -= OnDroppingItem;
        Player.Handcuffing -= OnHandcuffing;
        Player.Dying -= OnDying;
#else
        ServerEvents.WaveRespawning -= OnWaveRespawning;
        ServerEvents.WaveTeamSelecting -= OnWaveTeamSelecting;
        ServerEvents.LczDecontaminationStarting -= OnLczDecontaminationStarting;
        PlayerEvents.PlacingBulletHole -= OnPlayerPlacingBulletHole;
        ServerEvents.PickupCreated -= OnPickupCreated;
        PlayerEvents.SpawningRagdoll -= OnPlayerSpawningRagdoll;
        PlayerEvents.ShootingWeapon -= OnPlayerShootingWeapon;
        PlayerEvents.DroppingAmmo -= OnPlayerDroppingAmmo;
        PlayerEvents.DroppingItem -= OnPlayerDroppingItem;
        PlayerEvents.Cuffing -= OnPlayerCuffing;
        PlayerEvents.Dying -= OnPlayerDying;
#endif
    }

#if EXILED
    private void OnRespawningTeam(RespawningTeamEventArgs ev)
#else
    private void OnWaveRespawning(WaveRespawningEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRespawnTeam))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnSelectingRespawnTeam(SelectingRespawnTeamEventArgs ev)
#else
    private void OnWaveTeamSelecting(WaveTeamSelectingEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRespawnTeam))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnDecontaminating(DecontaminatingEventArgs ev)
#else
    private void OnLczDecontaminationStarting(LczDecontaminationStartingEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDecontaminating))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnPlacingBulletHole(PlacingBulletHoleEventArgs ev)
#else
    private void OnPlayerPlacingBulletHole(PlayerPlacingBulletHoleEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreBulletHole))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
#else
    private void OnPlayerSpawningRagdoll(PlayerSpawningRagdollEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreRagdoll))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnPickupAdded(PickupAddedEventArgs ev)
#else
    private void OnPickupCreated(PickupCreatedEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingAmmo) &&
                ev.Pickup.Type.GetName().Contains("Ammo"))
                ev.Pickup.Destroy();
    }
#if EXILED
    private void OnShooting(ShootingEventArgs ev)
#else
    private void OnPlayerShootingWeapon(PlayerShootingWeaponEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
        {
            if (activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreInfiniteAmmo))
                return;

            if (!Extensions.InfiniteAmmoList.ContainsKey(ev.Player))
                return;
#if EXILED
            if (ev.Firearm.Type is ItemType.ParticleDisruptor)
                return;

            ushort amount = 1;
            if (ev.Firearm.Type is ItemType.GunShotgun && ev.Firearm.HasAttachment(AttachmentName.ShotgunDoubleShot))
                amount = 2;
            else if (ev.Firearm.Type is ItemType.GunCom45) amount = 3;

            ev.Player.AddAmmo(ev.Firearm.AmmoType, amount);
#else
            if (ev.FirearmItem.Type is ItemType.ParticleDisruptor)
                return;

            ushort amount = 1;
            if (ev.FirearmItem.Type is ItemType.GunShotgun &&
                ev.FirearmItem.Base.Attachments.Any(a => a.Name == AttachmentName.ShotgunDoubleShot))
                amount = 2;
            else if (ev.FirearmItem.Type is ItemType.GunCom45) amount = 3;

            if (!ev.FirearmItem.Base.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
                return;
            ev.Player.AddAmmo(module.AmmoType, amount);
#endif
        }
    }
#if EXILED
    private void OnDroppingAmmo(DroppingAmmoEventArgs ev)
#else
    private void OnPlayerDroppingAmmo(PlayerDroppingAmmoEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingAmmo))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnDroppingItem(DroppingItemEventArgs ev)
#else
    private void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreDroppingItem))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnHandcuffing(HandcuffingEventArgs ev)
#else
    private void OnPlayerCuffing(PlayerCuffingEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is Event activeEvent)
            if (!activeEvent.EventHandlerSettings.HasFlag(EventFlags.IgnoreHandcuffing))
                ev.IsAllowed = false;
    }
#if EXILED
    private void OnDying(DyingEventArgs ev)
#else
    private void OnPlayerDying(PlayerDyingEventArgs ev)
#endif
    {
        if (AutoEvent.EventManager.CurrentEvent is null)
            return;

        if (!ev.IsAllowed)
            return;

        if (Extensions.InfiniteAmmoList is not null && Extensions.InfiniteAmmoList.ContainsKey(ev.Player))
            Extensions.InfiniteAmmoList.Remove(ev.Player);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API.Enums;
using UnityEngine;
using Utils.NonAllocLINQ;
#if EXILED
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Features.Wrappers;
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Jail;

public class EventHandler
{
    private readonly Plugin _plugin;

    public EventHandler(Plugin plugin)
    {
        _plugin = plugin;
    }

#if EXILED
    public void OnShooting(ShootingEventArgs ev)
    {
        if (!Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, out var raycastHit,
                100f, 1 << 0))
            return;

        if (Vector3.Distance(raycastHit.transform.gameObject.transform.position, _plugin.Button.transform.position) < 3)
        {
            ev.Player.ShowHitMarker(2f);
            _plugin.PrisonerDoors.GetComponent<JailerComponent>().ToggleDoor();
        }
    }
#else
    public void OnShooting(PlayerShootingWeaponEventArgs ev)
    {
        if (!Physics.Raycast(ev.Player.Camera.position, ev.Player.Camera.forward, out var raycastHit, 100f, 1 << 0))
            return;

        if (Vector3.Distance(raycastHit.transform.gameObject.transform.position, _plugin.Button.transform.position) < 3)
        {
            ev.Player.SendHitMarker(2f);
            _plugin.PrisonerDoors.GetComponent<JailerComponent>().ToggleDoor();
        }
    }
#endif
#if EXILED
    public void OnDying(DyingEventArgs ev)
#else
    public void OnDying(PlayerDyingEventArgs ev)
#endif
    {
        if (!ev.IsAllowed)
            return;

        if (_plugin.Deaths is null) _plugin.Deaths = new Dictionary<Player, int>();

        if (_plugin.Config.JailorLoadouts.Any(loadout => loadout.Roles.Any(role => role.Key == ev.Player.Role)))
            return;

        if (!_plugin.Deaths.ContainsKey(ev.Player)) _plugin.Deaths.Add(ev.Player, 1);
        if (_plugin.Deaths[ev.Player] >= _plugin.Config.PrisonerLives)
        {
#if EXILED
            ev.Player.ShowHint(_plugin.Translation.NoLivesRemaining, 4f);
#else
            ev.Player.SendHint(_plugin.Translation.NoLivesRemaining, 4f);
#endif
            return;
        }

        var livesRemaining = _plugin.Config.PrisonerLives = _plugin.Deaths[ev.Player];
#if EXILED
        ev.Player.ShowHint(_plugin.Translation.LivesRemaining.Replace("{lives}", livesRemaining.ToString()), 4f);
#else
        ev.Player.SendHint(_plugin.Translation.LivesRemaining.Replace("{lives}", livesRemaining.ToString()), 4f);
#endif
        ev.Player.GiveLoadout(_plugin.Config.PrisonerLoadouts);
        ev.Player.Position = _plugin.SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform
            .position;
    }

#if EXILED
    public void OnInteractingLocker(InteractingLockerEventArgs ev)
#else
    public void OnInteractingLocker(PlayerInteractingLockerEventArgs ev)
#endif
    {
        ev.IsAllowed = false;

        try
        {
            if (Vector3.Distance(ev.Player.Position,
                    _plugin.MapInfo.Map.Position + new Vector3(13.1f, -12.23f, -12.14f)) < 2)
            {
                ev.Player.ClearInventory();
                ev.Player.GiveLoadout(_plugin.Config.WeaponLockerLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.DontClearDefaultItems);
            }
            else if (Vector3.Distance(ev.Player.Position,
                         _plugin.MapInfo.Map.Position + new Vector3(17.855f, -12.43052f, -23.632f)) < 2)
            {
                ev.Player.GiveLoadout(_plugin.Config.AdrenalineLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons |
                    LoadoutFlags.DontClearDefaultItems);
            }
            else if (Vector3.Distance(ev.Player.Position,
                         _plugin.MapInfo.Map.Position + new Vector3(9f, -12.43052f, -21.78f)) < 2)
            {
                ev.Player.GiveLoadout(_plugin.Config.MedicalLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons |
                    LoadoutFlags.DontClearDefaultItems);
            }
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("An error has occured while processing locker events.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"{e}");
        }
    }
}
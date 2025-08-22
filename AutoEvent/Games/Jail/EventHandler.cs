using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace AutoEvent.Games.Jail;

public class EventHandler(Plugin plugin)
{
    public void OnShooting(PlayerShootingWeaponEventArgs ev)
    {
        if (!Physics.Raycast(ev.Player.Camera.position, ev.Player.Camera.forward, out var raycastHit, 100f, 1 << 0))
            return;

        if (!(Vector3.Distance(raycastHit.transform.gameObject.transform.position, plugin.Button.transform.position) <
              3)) return;
        ev.Player.SendHitMarker(2f);
        plugin.PrisonerDoors.GetComponent<JailerComponent>().ToggleDoor();
    }

    public void OnDying(PlayerDyingEventArgs ev)
    {
        if (!ev.IsAllowed)
            return;

        plugin.Deaths ??= new Dictionary<Player, int>();

        if (plugin.Config.JailorLoadouts.Any(loadout => loadout.Roles.Any(role => role.Key == ev.Player.Role)))
            return;

        if (!plugin.Deaths.ContainsKey(ev.Player)) plugin.Deaths.Add(ev.Player, 1);
        if (plugin.Deaths[ev.Player] >= plugin.Config.PrisonerLives)
        {
            ev.Player.SendHint(plugin.Translation.NoLivesRemaining, 4f);
            return;
        }

        var livesRemaining = plugin.Config.PrisonerLives = plugin.Deaths[ev.Player];
        ev.Player.SendHint(plugin.Translation.LivesRemaining.Replace("{lives}", livesRemaining.ToString()), 4f);
        ev.Player.GiveLoadout(plugin.Config.PrisonerLoadouts);
        ev.Player.Position = plugin.SpawnPoints.Where(r => r.name == "Spawnpoint").ToList().RandomItem().transform
            .position;
    }

    public void OnInteractingLocker(PlayerInteractingLockerEventArgs ev)
    {
        ev.IsAllowed = false;

        try
        {
            if (Vector3.Distance(ev.Player.Position,
                    plugin.MapInfo.Map.Position + new Vector3(13.1f, -12.23f, -12.14f)) < 2)
            {
                ev.Player.ClearInventory();
                ev.Player.GiveLoadout(plugin.Config.WeaponLockerLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.DontClearDefaultItems);
            }
            else if (Vector3.Distance(ev.Player.Position,
                         plugin.MapInfo.Map.Position + new Vector3(17.855f, -12.43052f, -23.632f)) < 2)
            {
                ev.Player.GiveLoadout(plugin.Config.AdrenalineLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons |
                    LoadoutFlags.DontClearDefaultItems);
            }
            else if (Vector3.Distance(ev.Player.Position,
                         plugin.MapInfo.Map.Position + new Vector3(9f, -12.43052f, -21.78f)) < 2)
            {
                ev.Player.GiveLoadout(plugin.Config.MedicalLoadouts,
                    LoadoutFlags.IgnoreRole | LoadoutFlags.IgnoreGodMode | LoadoutFlags.IgnoreWeapons |
                    LoadoutFlags.DontClearDefaultItems);
            }
        }
        catch (Exception e)
        {
            LogManager.Error("An error has occured while processing locker events.");
            LogManager.Error($"{e}");
        }
    }
}
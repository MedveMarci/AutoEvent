using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdminToys;
using AutoEvent.API.Enums;
using AutoEvent.ApiFeatures;
using AutoEvent.Integrations.Audio;
using AutoEvent.Integrations.MapEditor;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using ThrowableItem = InventorySystem.Items.ThrowableProjectiles.ThrowableItem;

namespace AutoEvent.API;

public static class Extensions
{
    public enum LoadoutCheckMethods
    {
        HasRole,
        HasSomeItems,
        HasAllItems
    }

    public static readonly Dictionary<uint, AmmoMode> InfiniteAmmoList = new();
    public static readonly List<uint> InfinityStaminaList = [];

    private static readonly ConcurrentDictionary<ulong, float> InteractableToys = new();

    public static void TeleportEnd()
    {
        foreach (var player in Player.ReadyList)
        {
            player.SetRole(AutoEvent.Singleton.Config.LobbyRole);
            player.GiveInfiniteAmmo(AmmoMode.None);
            player.IsGodModeEnabled = false;
            player.Scale = new Vector3(1, 1, 1);
            player.Position = new Vector3(39.332f, 314.112f, -31.922f);
            InfinityStaminaList.Remove(player.NetworkId);
            player.ClearInventory();
        }
    }

    public static bool IsExistsMap(string schematicName, out string response)
    {
        if (ProjectMerIntegration.IsExistsMap(schematicName))
        {
            response = $"The map {schematicName} exist and can be used.";
            return true;
        }

        response = $"You need to download the {schematicName} map to run this mini-game.\n" +
                   $"Download and install Schematics.tar.gz from the github.";
        return false;
    }

    public static MapObject LoadMap(string schematicName, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        try
        {
            return ProjectMerIntegration.LoadMap(schematicName, pos, rot, scale);
        }
        catch (Exception e)
        {
            LogManager.Error($"An error occured at LoadMap.\n{e}");
        }

        return null;
    }

    public static void UnLoadMap(MapObject mapObject)
    {
        mapObject?.Destroy();
    }

    public static void CleanUpAll()
    {
        foreach (var item in Object.FindObjectsByType<ItemPickupBase>(FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
            Object.Destroy(item.gameObject);

        foreach (var ragdoll in Object.FindObjectsByType<BasicRagdoll>(FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
            Object.Destroy(ragdoll.gameObject);
    }

    public static void ServerBroadcast(string text, ushort time)
    {
        Server.SendBroadcast(text, time, global::Broadcast.BroadcastFlags.Normal, true);
    }

    public static void GrenadeSpawn(Vector3 pos, float scale = 1f, float fuseTime = 1f, float radius = 5f)
    {
        if (!InventoryItemLoader.TryGetItem(ItemType.GrenadeHE, out ThrowableItem result))
            return;
        if (result.Projectile is not TimeGrenade projectile)
            return;
        var timeGrenade = Object.Instantiate(projectile, pos, Quaternion.identity);
        timeGrenade.Info = new PickupSyncInfo(result.ItemTypeId, result.Weight, locked: true);
        timeGrenade.PreviousOwner = new Footprint(Player.Host?.ReferenceHub);
        timeGrenade.gameObject.transform.localScale = new Vector3(scale, scale, scale);
        NetworkServer.Spawn(timeGrenade.gameObject);
        timeGrenade.ServerActivate();
        var grenadeProjectile = (ExplosiveGrenadeProjectile)Pickup.Get(timeGrenade);
        grenadeProjectile.RemainingTime = fuseTime;
        grenadeProjectile.MaxRadius = radius;
        grenadeProjectile.Base._playerDamageOverDistance =
            new AnimationCurve(new Keyframe(grenadeProjectile.MaxRadius, 200));
    }

    /// <summary>
    ///     Plays an audio file using the active audio backend.
    /// </summary>
    public static IAudioHandle PlayAudio(string fileName, bool isLoop = false, bool isSpatial = false,
        float minDistance = 5f, float maxDistance = 5000f, Vector3 speakerPosition = default)
    {
        var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
        LogManager.Debug($"[PlayAudio] File path: {filePath}");
        if (!File.Exists(filePath))
        {
            LogManager.Debug($"[PlayAudio] The music file {fileName} does not exist at path {filePath}");
            return null;
        }

#if APAPI
        return AudioPlayerApiIntegration.PlayAudio(filePath, isLoop, isSpatial, minDistance, maxDistance, speakerPosition);
#else
        return SlNaIntegration.PlayAudio(filePath, isLoop, isSpatial, minDistance, maxDistance, speakerPosition);
#endif
    }

    /// <summary>
    ///     Plays an audio file audible only to a specific player using the active audio backend.
    /// </summary>
    public static IAudioHandle PlayPlayerAudio(Player player, string fileName, bool isLoop = false)
    {
        var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
        LogManager.Debug($"[PlayPlayerAudio] File path: {filePath}");
        if (!File.Exists(filePath))
        {
            LogManager.Debug($"[PlayPlayerAudio] The music file {fileName} does not exist at path {filePath}");
            return null;
        }

#if APAPI
        return AudioPlayerApiIntegration.PlayPlayerAudio(player, filePath, isLoop);
#else
        return SlNaIntegration.PlayPlayerAudio(player, filePath, isLoop);
#endif
    }

    extension(Player ply)
    {
        public bool HasLoadout(List<Loadout> loadouts,
            LoadoutCheckMethods checkMethod = LoadoutCheckMethods.HasRole)
        {
            switch (checkMethod)
            {
                case LoadoutCheckMethods.HasRole:
                    return loadouts.Any(loadout => loadout.Roles.Any(role => role.Key == ply.Role));
                case LoadoutCheckMethods.HasAllItems:
                    return loadouts.Any(loadout =>
                        loadout.Items.All(item => ply.Items.Select(itm => itm.Type).Contains(item)));
                case LoadoutCheckMethods.HasSomeItems:
                    return loadouts.Any(loadout =>
                        loadout.Items.Any(item => ply.Items.Select(itm => itm.Type).Contains(item)));
            }

            return false;
        }

        public void GiveLoadout(List<Loadout> loadouts, LoadoutFlags flags = LoadoutFlags.None)
        {
            Loadout loadout;
            if (loadouts.Count == 1)
            {
                loadout = loadouts[0];
                goto assignLoadout;
            }

            foreach (var loadout1 in loadouts.Where(x => x.Chance <= 0))
                loadout1.Chance = 1;

            var totalChance = loadouts.Sum(x => x.Chance);

            for (var i = 0; i < loadouts.Count - 1; i++)
                if (Random.Range(0, totalChance) <= loadouts[i].Chance)
                {
                    loadout = loadouts[i];
                    goto assignLoadout;
                }

            loadout = loadouts[loadouts.Count - 1];
            assignLoadout:
            ply.GiveLoadout(loadout, flags);
        }

        public void GiveLoadout(Loadout loadout, LoadoutFlags flags = LoadoutFlags.None)
        {
            var respawnFlags = RoleSpawnFlags.None;
            if (loadout.Roles is not null && loadout.Roles.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreRole))
            {
                if (flags.HasFlag(LoadoutFlags.UseDefaultSpawnPoint))
                    respawnFlags |= RoleSpawnFlags.UseSpawnpoint;
                if (flags.HasFlag(LoadoutFlags.DontClearDefaultItems))
                    respawnFlags |= RoleSpawnFlags.AssignInventory;

                RoleTypeId role;
                if (loadout.Roles.Count == 1)
                {
                    role = loadout.Roles.First().Key;
                }
                else
                {
                    var list = loadout.Roles.ToList();
                    var roleTotalChance = list.Sum(x => x.Value);
                    for (var i = 0; i < list.Count - 1; i++)
                        if (Random.Range(0, roleTotalChance) <= list[i].Value)
                        {
                            role = list[i].Key;
                            goto assignRole;
                        }

                    role = list[list.Count - 1].Key;
                }

                assignRole:
                if (AutoEvent.Singleton.Config.IgnoredRoles.Contains(role))
                {
                    LogManager.Warn(
                        "AutoEvent is trying to set a player to a role that is apart of IgnoreRoles. This is probably an error. The plugin will instead set players to the lobby role to prevent issues.");
                    role = AutoEvent.Singleton.Config.LobbyRole;
                }

                ply.SetRole(role, flags: respawnFlags);
            }

            if (!flags.HasFlag(LoadoutFlags.DontClearDefaultItems)) ply.ClearInventory();

            if (loadout.Items is not null && loadout.Items.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreItems))
                foreach (var item in loadout.Items)
                {
                    if (flags.HasFlag(LoadoutFlags.IgnoreWeapons) && item.ToString().Contains("Gun"))
                        continue;

                    ply.AddItem(item);
                }

            if ((loadout.InfiniteAmmo != AmmoMode.None && !flags.HasFlag(LoadoutFlags.IgnoreInfiniteAmmo)) ||
                flags.HasFlag(LoadoutFlags.ForceInfiniteAmmo) ||
                flags.HasFlag(LoadoutFlags.ForceEndlessClip)) ply.GiveInfiniteAmmo(AmmoMode.InfiniteAmmo);
            if (loadout.Health != 0 && !flags.HasFlag(LoadoutFlags.IgnoreHealth))
                ply.Health = loadout.Health;
            if (loadout.Health == -1 && !flags.HasFlag(LoadoutFlags.IgnoreGodMode)) ply.IsGodModeEnabled = true;

            if (loadout.ArtificialHealth is not null && loadout.ArtificialHealth.MaxAmount > 0 &&
                !flags.HasFlag(LoadoutFlags.IgnoreAhp)) loadout.ArtificialHealth.ApplyToPlayer(ply);

            if (!flags.HasFlag(LoadoutFlags.IgnoreStamina) && loadout.Stamina != 0)
            {
                ply.StaminaRemaining = loadout.Stamina;
            }
            else
            {
                if (!InfinityStaminaList.Contains(ply.NetworkId))
                    InfinityStaminaList.Add(ply.NetworkId);
            }

            if (loadout.Size != Vector3.one && !flags.HasFlag(LoadoutFlags.IgnoreSize)) ply.Scale = loadout.Size;

            if (loadout.Effects is null || loadout.Effects.Count <= 0 ||
                flags.HasFlag(LoadoutFlags.IgnoreEffects)) return;
            foreach (var effect in loadout.Effects)
            {
                if (!ply.TryGetEffect(effect.Type, out var customEffect)) continue;
                customEffect.Intensity = effect.Intensity;
                customEffect.Duration = effect.Duration;
            }
        }

        public void GiveInfiniteAmmo(AmmoMode ammoMode)
        {
            LogManager.Debug(
                $"Setting infinite ammo mode for player {ply.Nickname} ({ply.NetworkId}) to {ammoMode}");
            if (ammoMode == AmmoMode.None)
                InfiniteAmmoList.Remove(ply.NetworkId);
            else
                InfiniteAmmoList[ply.NetworkId] = ammoMode;
        }

        public void Broadcast(string text, ushort time = 3)
        {
            ply.SendBroadcast(text, time, global::Broadcast.BroadcastFlags.Normal, true);
        }
    }

    extension(IAudioHandle handle)
    {
        public void PauseAudio()
        {
            if (handle is null)
            {
                LogManager.Debug("[PauseAudio] The audio handle is null");
                return;
            }

            handle.IsPaused = true;
        }

        public void ResumeAudio()
        {
            if (handle is null)
            {
                LogManager.Debug("[ResumeAudio] The audio handle is null");
                return;
            }

            handle.IsPaused = false;
        }

        public void StopAudio()
        {
            if (handle is null)
            {
                LogManager.Debug("[StopAudio] The audio handle is null");
                return;
            }

            handle.Stop();
        }
    }

    extension(InvisibleInteractableToy toy)
    {
        private ulong Key(uint playerNetId)
        {
            return ((ulong)toy.netIdentity.netId << 32) | playerNetId;
        }

        public void SetInteractableToy(Player player, float duration)
        {
            if (toy == null || player == null) return;
            InteractableToys[toy.Key(player.NetworkId)] = duration;
        }

        public bool TryGetInteractableToy(ReferenceHub hub, out float duration)
        {
            duration = 0;
            if (toy == null || hub == null) return false;
            return InteractableToys.TryGetValue(toy.Key(hub.networkIdentity.netId), out duration);
        }

        public void ClearInteractableToy()
        {
            if (toy == null) return;
            var toyId = toy.netIdentity.netId;
            foreach (var k in InteractableToys.Keys.Where(k => k >> 32 == toyId))
                InteractableToys.TryRemove(k, out _);
        }
    }
}
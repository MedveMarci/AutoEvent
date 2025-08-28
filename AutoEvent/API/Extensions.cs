using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdminToys;
using AutoEvent.API.Enums;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
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

    public static readonly Dictionary<string, AmmoMode> InfiniteAmmoList = new();
    public static readonly List<string> InfinityStaminaList = [];

    public static bool HasLoadout(this Player ply, List<Loadout> loadouts,
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

    public static void GiveLoadout(this Player player, List<Loadout> loadouts, LoadoutFlags flags = LoadoutFlags.None)
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
        GiveLoadout(player, loadout, flags);
    }

    public static void GiveLoadout(this Player player, Loadout loadout, LoadoutFlags flags = LoadoutFlags.None)
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

            player.SetRole(role, flags: respawnFlags);
        }

        if (!flags.HasFlag(LoadoutFlags.DontClearDefaultItems)) player.ClearInventory();

        if (loadout.Items is not null && loadout.Items.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreItems))
            foreach (var item in loadout.Items)
            {
                if (flags.HasFlag(LoadoutFlags.IgnoreWeapons) && item is Firearm)
                    continue;

                player.AddItem(item);
            }

        if ((loadout.InfiniteAmmo != AmmoMode.None && !flags.HasFlag(LoadoutFlags.IgnoreInfiniteAmmo)) ||
            flags.HasFlag(LoadoutFlags.ForceInfiniteAmmo) ||
            flags.HasFlag(LoadoutFlags.ForceEndlessClip)) player.GiveInfiniteAmmo(AmmoMode.InfiniteAmmo);
        if (loadout.Health != 0 && !flags.HasFlag(LoadoutFlags.IgnoreHealth))
            player.Health = loadout.Health;
        if (loadout.Health == -1 && !flags.HasFlag(LoadoutFlags.IgnoreGodMode)) player.IsGodModeEnabled = true;

        if (loadout.ArtificialHealth is not null && loadout.ArtificialHealth.MaxAmount > 0 &&
            !flags.HasFlag(LoadoutFlags.IgnoreAhp)) loadout.ArtificialHealth.ApplyToPlayer(player);

        if (!flags.HasFlag(LoadoutFlags.IgnoreStamina) && loadout.Stamina != 0)
        {
            player.ReferenceHub.playerStats.GetModule<StaminaStat>().ModifyAmount(loadout.Stamina);
        }
        else
        {
            if (!InfinityStaminaList.Contains(player.UserId))
                InfinityStaminaList.Add(player.UserId);
        }

        if (loadout.Size != Vector3.one && !flags.HasFlag(LoadoutFlags.IgnoreSize)) player.Scale = loadout.Size;

        if (loadout.Effects is null || loadout.Effects.Count <= 0 || flags.HasFlag(LoadoutFlags.IgnoreEffects)) return;
        foreach (var effect in loadout.Effects)
        {
            if (!player.TryGetEffect(effect.Type, out var customEffect)) continue;
            customEffect.Intensity = effect.Intensity;
            customEffect.Duration = effect.Duration;
        }
    }

    public static void GiveInfiniteAmmo(this Player player, AmmoMode ammoMode)
    {
        if (ammoMode == AmmoMode.None)
            if (InfiniteAmmoList is null || InfiniteAmmoList.Count < 1 || !InfiniteAmmoList.Remove(player.UserId))
                return;

        InfiniteAmmoList[player.UserId] = ammoMode;
        foreach (ItemType ammoType in Enum.GetValues(typeof(ItemType)))
        {
            if (ammoType == ItemType.None)
                continue;

            if (!nameof(ammoType).Contains("Ammo"))
                return;

            player.SetAmmo(ammoType, 100);
        }
    }

    public static void TeleportEnd()
    {
        foreach (var player in Player.ReadyList)
        {
            player.SetRole(AutoEvent.Singleton.Config.LobbyRole);
            player.GiveInfiniteAmmo(AmmoMode.None);
            player.IsGodModeEnabled = false;
            player.Scale = new Vector3(1, 1, 1);
            player.Position = new Vector3(39.332f, 314.112f, -31.922f);
            InfinityStaminaList.Remove(player.UserId);
            player.ClearInventory();
        }
    }

    public static bool IsExistsMap(string schematicName, out string response)
    {
        try
        {
            response = $"The map {schematicName} exist and can be used.";
            return true;
        }
        catch (Exception)
        {
            // The old version of ProjectMER is installed
            if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.ToLower().Contains("projectmer")))
            {
                response = $"You need to download the map {schematicName} to run this mini-game.\n" +
                           $"Download and install Schematics.tar.gz from the github.";
                return false;
            }
        }

        // The MER is not installed
        response = $"You need to download the 'ProjectMER' to run this mini-game.\n" +
                   $"Read the installation instruction in the github.";
        return false;
    }

    public static MapObject LoadMap(string schematicName, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        try
        {
            var schematicObject = ObjectSpawner.SpawnSchematic(schematicName, pos, rot, scale);

            foreach (var toyBase in schematicObject.AdminToyBases)
                toyBase.syncInterval = 0;
            
            return new MapObject
            {
                AttachedBlocks = schematicObject.AttachedBlocks.ToList(),
                AdminToyBases = schematicObject.AdminToyBases.ToList(),
                GameObject = schematicObject.gameObject
            };
        }
        catch (Exception e)
        {
            LogManager.Error($"An error occured at LoadMap.\n{e}");
        }

        return null;
    }

    public static GameObject CreatePlatformByParent(GameObject parent, Vector3 position)
    {
        var prim = parent.GetComponent<PrimitiveObjectToy>();
        var obj = ObjectSpawner.SpawnPrimitive(new SerializablePrimitive
        {
            PrimitiveType = prim.PrimitiveType,
            Position = position,
            Scale = parent.transform.localScale,
            Color = prim.MaterialColor.ToHex()
        });

        NetworkServer.Spawn(obj.gameObject);
        return obj.gameObject;
    }

    public static void UnLoadMap(MapObject mapObject)
    {
        mapObject.Destroy();
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

    public static void Broadcast(this Player player, string text, ushort time)
    {
        player.SendBroadcast(text, time, global::Broadcast.BroadcastFlags.Normal, true);
    }


    public static void GrenadeSpawn(Vector3 pos, float scale = 1f, float fuseTime = 1f, float radius = 1f)
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
        var grenadeProjectile = (ExplosiveGrenadeProjectile)Pickup.Get(timeGrenade);
        grenadeProjectile.RemainingTime = fuseTime;
        grenadeProjectile.MaxRadius = radius;
        timeGrenade.ServerActivate();
    }

    public static AudioPlayer PlayAudio(string fileName, byte volume, bool isLoop, bool isSpatial = false,
        float minDistance = 5f, float maxDistance = 5000f)
    {
        if (!AudioClipStorage.AudioClips.ContainsKey(fileName))
        {
            var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
            LogManager.Debug($"[PlayAudio] File path: {filePath}");
            if (!AudioClipStorage.LoadClip(filePath, fileName))
            {
                LogManager.Debug($"[PlayAudio] The music file {fileName} was not found for playback");
                return null;
            }
        }

        var audioPlayer = AudioPlayer.CreateOrGet("AutoEvent-Global",
            onIntialCreation: p =>
            {
                p.AddSpeaker($"AutoEvent-Main-{fileName}", volume * (AutoEvent.Singleton.Config.Volume / 100f),
                    isSpatial, minDistance, maxDistance);
            });

        audioPlayer.SendSoundGlobally = true;
        audioPlayer.AddClip(fileName, loop: isLoop);

        return audioPlayer;
    }

    public static void PlayPlayerAudio(AudioPlayer audioPlayer, Player player, string fileName, byte volume)
    {
        if (audioPlayer is null)
        {
            LogManager.Debug("[PlayPlayerAudio] The AudioPlayer is null");
            return;
        }

        if (player is null) LogManager.Debug("[PlayPlayerAudio] The player is null");

        if (!AudioClipStorage.AudioClips.ContainsKey(fileName))
        {
            var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
            LogManager.Debug($"[PlayPlayerAudio] File path: {filePath}");
            if (!AudioClipStorage.LoadClip(filePath, fileName))
            {
                LogManager.Debug($"[PlayPlayerAudio] The music file {fileName} was not found for playback");
                return;
            }
        }

        audioPlayer.AddClip(fileName);
    }

    public static void PauseAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null)
        {
            LogManager.Debug("[PauseAudio] The AudioPlayer is null");
            return;
        }

        var clipId = audioPlayer.ClipsById.Keys.First();
        if (audioPlayer.TryGetClip(clipId, out var clip)) clip.IsPaused = true;
    }

    public static void ResumeAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null)
        {
            LogManager.Debug("[PauseAudio] The AudioPlayer is null");
            return;
        }

        var clipId = audioPlayer.ClipsById.Keys.First();
        if (audioPlayer.TryGetClip(clipId, out var clip)) clip.IsPaused = false;
    }

    public static void StopAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null)
        {
            LogManager.Debug("[StopAudio] The AudioPlayer is null");
            return;
        }

        audioPlayer.RemoveAllClips();
        audioPlayer.Destroy();
    }
}
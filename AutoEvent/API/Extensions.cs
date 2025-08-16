using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoEvent.API;
using AutoEvent.API.Enums;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;
using JailbirdItem = InventorySystem.Items.Jailbird.JailbirdItem;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Random = UnityEngine.Random;

#if EXILED
using Player = Exiled.API.Features.Player;
using Item = Exiled.API.Features.Items.Item;
using Map = Exiled.API.Features.Map;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;

#else
using InventorySystem.Items.Firearms;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent;

public static class Extensions
{
    public enum LoadoutCheckMethods
    {
        HasRole,
        HasSomeItems,
        HasAllItems
    }

    public static Dictionary<Player, AmmoMode> InfiniteAmmoList = new();
    public static bool JailbirdIsInvincible { get; set; } = true;
    public static List<JailbirdItem> InvincibleJailbirds { get; set; } = new();

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
        var role = RoleTypeId.None;
        var respawnFlags = RoleSpawnFlags.None;
        if (loadout.Roles is not null && loadout.Roles.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreRole))
        {
            if (flags.HasFlag(LoadoutFlags.UseDefaultSpawnPoint))
                respawnFlags |= RoleSpawnFlags.UseSpawnpoint;
            if (flags.HasFlag(LoadoutFlags.DontClearDefaultItems))
                respawnFlags |= RoleSpawnFlags.AssignInventory;

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
                DebugLogger.LogDebug(
                    "AutoEvent is trying to set a player to a role that is apart of IgnoreRoles. This is probably an error. The plugin will instead set players to the lobby role to prevent issues.",
                    LogLevel.Error, true);
                role = AutoEvent.Singleton.Config.LobbyRole;
            }
#if EXILED
            player.Role.Set(role, respawnFlags);
#else
            player.SetRole(role, flags: respawnFlags);
#endif
        }

        if (!flags.HasFlag(LoadoutFlags.DontClearDefaultItems)) player.ClearInventory();

        if (loadout.Items is not null && loadout.Items.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreItems))
            foreach (var item in loadout.Items)
            {
#if EXILED
                if (flags.HasFlag(LoadoutFlags.IgnoreWeapons) && item.IsWeapon())
#else
                if (flags.HasFlag(LoadoutFlags.IgnoreWeapons) && item is Firearm)
#endif
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
            !flags.HasFlag(LoadoutFlags.IgnoreAHP)) loadout.ArtificialHealth.ApplyToPlayer(player);

        if (!flags.HasFlag(LoadoutFlags.IgnoreStamina) && loadout.Stamina != 0)
        {
            player.ReferenceHub.playerStats.GetModule<StaminaStat>().ModifyAmount(loadout.Stamina);
        }
        else
        {
#if EXILED
            player.IsUsingStamina = false;
#else
            player.EnableEffect<Invigorated>();
#endif
        }

        if (loadout.Size != Vector3.one && !flags.HasFlag(LoadoutFlags.IgnoreSize)) player.Scale = loadout.Size;

        if (loadout.Effects is not null && loadout.Effects.Count > 0 && !flags.HasFlag(LoadoutFlags.IgnoreEffects))
            foreach (var effect in loadout.Effects)
            {
#if EXILED
                player.EnableEffect(effect.Type, effect.Intensity, effect.Duration);
#else
                var effectType = Type.GetType($"CustomPlayerEffects.{effect}");
                if (effectType != null && typeof(StatusEffectBase).IsAssignableFrom(effectType))
                    player.EnableEffect((StatusEffectBase)Activator.CreateInstance(effectType), effect.Intensity,
                        effect.Duration);
#endif
            }
    }

    public static void SetPlayerAhp(this Player player, float amount, float limit = 75, float decay = 1.2f,
        float efficacy = 0.7f, float sustain = 0, bool persistant = false)
    {
        if (amount > 100) amount = 100;

        player.ReferenceHub.playerStats.GetModule<AhpStat>()
            .ServerAddProcess(amount, limit, decay, efficacy, sustain, persistant);
    }

    public static void GiveInfiniteAmmo(this Player player, AmmoMode ammoMode)
    {
        if (ammoMode == AmmoMode.None)
            if (InfiniteAmmoList is null || InfiniteAmmoList.Count < 1 || !InfiniteAmmoList.Remove(player))
                return;

        InfiniteAmmoList[player] = ammoMode;
#if EXILED
        foreach (AmmoType ammoType in Enum.GetValues(typeof(AmmoType)))
        {
            if (ammoType == AmmoType.None)
                continue;

            player.SetAmmo(ammoType, 100);
        }
#else
        foreach (ItemType ammoType in Enum.GetValues(typeof(ItemType)))
        {
            if (ammoType == ItemType.None)
                continue;

            if (!nameof(ammoType).Contains("Ammo"))
                return;

            player.SetAmmo(ammoType, 100);
        }
#endif
    }

    public static void TeleportEnd()
    {
#if EXILED
        foreach (var player in Player.List)
        {
            player.Role.Set(AutoEvent.Singleton.Config.LobbyRole);
#else
        foreach (var player in Player.ReadyList)
        {
            player.SetRole(AutoEvent.Singleton.Config.LobbyRole);
#endif
            player.GiveInfiniteAmmo(AmmoMode.None);
            player.IsGodModeEnabled = false;
            player.Scale = new Vector3(1, 1, 1);
            player.Position = new Vector3(39.332f, 314.112f, -31.922f);
            player.ClearInventory();
        }
    }

    public static bool IsExistsMap(string schematicName, out string response)
    {
        try
        {
            if (MapUtils.GetSchematicDataByName(schematicName) is null)
            {
                // Map is not installed for ProjectMER
                response = $"You need to download the map {schematicName} to run this mini-game.\n" +
                           $"Download and install Schematics.tar.gz from the github.";
                return false;
            }

            // The latest ProjectMER and Schematics are installed
            response = $"The map {schematicName} exist and can be used.";
            return true;
        }
        catch (Exception _)
        {
            // The old version of ProjectMER is installed
            if (AppDomain.CurrentDomain.GetAssemblies().Any(x => x.FullName.ToLower().Contains("projectmer")))
            {
                response = $"You have installed the old version of 'ProjectMER' and cannot run this mini-game.\n" +
                           $"Install the latest version of 'ProjectMER'.";
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

            return new MapObject
            {
                AttachedBlocks = schematicObject.AttachedBlocks.ToList(),
                GameObject = schematicObject.gameObject
            };
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("An error occured at LoadMap.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"{e}");
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
        foreach (var item in Object.FindObjectsOfType<ItemPickupBase>()) Object.Destroy(item.gameObject);

        foreach (var ragdoll in Object.FindObjectsOfType<BasicRagdoll>()) Object.Destroy(ragdoll.gameObject);
    }

    public static void Broadcast(string text, ushort time)
    {
#if EXILED
        Map.ClearBroadcasts();
        Map.Broadcast(time, text);
#else
        Server.ClearBroadcasts();
        Server.SendBroadcast(text, time);
#endif
    }

    public static void GrenadeSpawn(Vector3 pos, float scale = 1f, float fuseTime = 1f, float radius = 1f)
    {
#if EXILED
        var grenade = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE);
        grenade.Scale = new Vector3(scale, scale, scale);
        grenade.FuseTime = fuseTime;
        grenade.MaxRadius = radius;
        grenade.SpawnActive(pos);
#else
        var grenade =
            Pickup.Create(ItemType.GrenadeHE, pos, Quaternion.identity, new Vector3(scale, scale, scale)) as
                ExplosiveGrenadeProjectile;
        if (grenade == null) return;
        grenade.MaxRadius = radius;
        grenade.Base._fuseTime = fuseTime;
        grenade.Spawn();
#endif
    }

    public static AudioPlayer PlayAudio(string fileName, byte volume, bool isLoop)
    {
        if (!AudioClipStorage.AudioClips.ContainsKey(fileName))
        {
            var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
            DebugLogger.LogDebug($"{filePath}");
            if (!AudioClipStorage.LoadClip(filePath, fileName))
            {
                DebugLogger.LogDebug($"[PlayAudio] The music file {fileName} was not found for playback");
                return null;
            }
        }

        var audioPlayer = AudioPlayer.CreateOrGet("AutoEvent-Global", onIntialCreation: p =>
        {
            var speaker = p.AddSpeaker($"AutoEvent-Main-{fileName}", isSpatial: false, maxDistance: 5000f);
            speaker.Volume = volume * (AutoEvent.Singleton.Config.Volume / 100f);
        });

        audioPlayer.SendSoundGlobally = true;
        audioPlayer.AddClip(fileName, loop: isLoop);

        return audioPlayer;
    }

    public static void PlayPlayerAudio(AudioPlayer audioPlayer, Player player, string fileName, byte volume)
    {
        if (audioPlayer is null) DebugLogger.LogDebug("[PlayPlayerAudio] The AudioPlayer is null");

        if (player is null) DebugLogger.LogDebug("[PlayPlayerAudio] The player is null");

        if (!AudioClipStorage.AudioClips.ContainsKey(fileName))
        {
            var filePath = Path.Combine(AutoEvent.Singleton.Config.MusicDirectoryPath, fileName);
            DebugLogger.LogDebug($"{filePath}");
            if (!AudioClipStorage.LoadClip(filePath, fileName))
            {
                DebugLogger.LogDebug($"[PlayPlayerAudio] The music file {fileName} was not found for playback");
                return;
            }
        }

        audioPlayer.AddClip(fileName);
    }

    public static void PauseAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null) DebugLogger.LogDebug("[PauseAudio] The AudioPlayer is null");

        try
        {
            var clipId = audioPlayer.ClipsById.Keys.First();
            if (audioPlayer.TryGetClip(clipId, out var clip)) clip.IsPaused = true;
        }
        catch
        {
        }
    }

    public static void ResumeAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null) DebugLogger.LogDebug("[PauseAudio] The AudioPlayer is null");

        try
        {
            var clipId = audioPlayer.ClipsById.Keys.First();
            if (audioPlayer.TryGetClip(clipId, out var clip)) clip.IsPaused = false;
        }
        catch
        {
        }
    }

    public static void StopAudio(AudioPlayer audioPlayer)
    {
        if (audioPlayer is null) DebugLogger.LogDebug("[StopAudio] The AudioPlayer is null");

        try
        {
            audioPlayer.RemoveAllClips();
            audioPlayer.Destroy();
        }
        catch (Exception e)
        {
            DebugLogger.LogDebug("An error occured at StopAudio.", LogLevel.Warn, true);
            DebugLogger.LogDebug($"{e}");
        }
    }
}
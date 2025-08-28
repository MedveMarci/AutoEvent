using System;
using AutoEvent.Games.CounterStrike.Features;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;
using Extensions = AutoEvent.API.Extensions;

namespace AutoEvent.Games.CounterStrike;

public class EventHandler(Plugin plugin)
{
    internal static Bounds BSiteBounds = new();
    internal static Bounds ASiteBounds = new();
    internal static InteractableToy Button;
    internal static Scp1576Item Bomb;
    private static AudioPlayer _bombAudio;
    
    public void OnSearchedToy(PlayerSearchedToyEventArgs ev)
    {
        LogManager.Debug("Player interacted with the bomb");
        if (plugin.BombState != BombState.Planted) return;
        if (!ev.Player.IsNTF) return;
        LogManager.Debug("Player is defusing the bomb");
        plugin.BombState = BombState.Defused;
        ev.Player.SendHint(plugin.Translation.YouDefused);
        ev.Player.DisableEffect<Ensnared>();
        ev.Player.DisableEffect<HeavyFooted>();
        _bombAudio.Destroy();
        var lightSource = plugin.BombObject.transform.Find("Bomb_Source/LightSource");
        NetworkServer.Destroy(lightSource.gameObject);
    }
    
    public static void OnSearchToyAborted(PlayerSearchToyAbortedEventArgs ev)
    {
        ev.Player.DisableEffect<Ensnared>();
        ev.Player.DisableEffect<HeavyFooted>();
    }

    public static void OnSearchingToy(PlayerSearchingToyEventArgs ev)
    {
        if (ev.Player.IsChaos)
        {
            ev.IsAllowed = false;
            return;
        }
        
        ev.Player.EnableEffect<Ensnared>(255);
        ev.Player.EnableEffect<HeavyFooted>(255);
    }

    public static void OnUsingItem(PlayerUsingItemEventArgs ev)
    {
        LogManager.Debug("Player is trying to use an item");
        if (ev.UsableItem.Base == null || Bomb == null || ev.UsableItem.Base.ItemId != Bomb.Base.ItemId) return;
        LogManager.Debug(ev.Player.Position.ToString());
        LogManager.Debug(ASiteBounds.Contains(ev.Player.Position).ToString());
        LogManager.Debug(BSiteBounds.Contains(ev.Player.Position).ToString());
        LogManager.Debug(ASiteBounds.center.ToString());
        LogManager.Debug(BSiteBounds.center.ToString());
        if (!ASiteBounds.Contains(ev.Player.Position) && !BSiteBounds.Contains(ev.Player.Position))
        {
            LogManager.Debug("Player is not in a bomb site");
            ev.IsAllowed = false;
            ev.UsableItem.GlobalCooldownDuration = 0f;
            ev.UsableItem.PersonalCooldownDuration = 0f;
            return;
        }
        LogManager.Debug("Player is in a bomb site");
        ev.UsableItem.MaxCancellableDuration = 20f;
        ev.Player.EnableEffect<Ensnared>(255);
        ev.Player.EnableEffect<HeavyFooted>(255);
    }

    public void OnUsedItem(PlayerUsedItemEventArgs ev)
    {
        if (ev.UsableItem.Base.ItemId != Bomb.Base.ItemId) return;
        if (!ev.Player.IsChaos) return;
        ev.UsableItem.GlobalCooldownDuration = 0f;
        ev.UsableItem.PersonalCooldownDuration = 0f;
        if (!ASiteBounds.Contains(ev.Player.Position) && !BSiteBounds.Contains(ev.Player.Position))
            return;
        Button.IsLocked = false;
        plugin.BombState = BombState.Planted;
        plugin.RoundTime = new TimeSpan(0, 0, 35);
        plugin.BombObject.transform.parent = null;
        plugin.BombObject.transform.ResetTransform();
        plugin.BombObject.transform.position = ev.Player.Position + new Vector3(0f, -1f, -0.75f);
        _bombAudio = Extensions.PlayAudio("BombPlanted.ogg", 5, false, true, 10, 20);
        ev.Player.SendHint(plugin.Translation.YouPlanted);
        ev.Player.DisableEffect<Ensnared>();
        ev.Player.DisableEffect<HeavyFooted>();
        ev.Player.RemoveItem(ItemType.SCP1576);
    }

    public static void OnCancelledUsingItem(PlayerCancelledUsingItemEventArgs ev)
    {
        if (ev.UsableItem.Base.ItemId != Bomb.Base.ItemId) return;
        Bomb.PersonalCooldownDuration = 0f;
        Bomb.GlobalCooldownDuration = 0f;
        ev.Player.DisableEffect<Ensnared>();
        ev.Player.DisableEffect<HeavyFooted>();
    }
    
    public static void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        if (ev.Pickup.Base.ItemId != Bomb.Base.ItemId) return;
        if (ev.Player.IsNTF) ev.IsAllowed = false;
    }
    
    public void OnPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        if (ev.Pickup.Base.ItemId != Bomb.Base.ItemId) return;
        ev.Player.SendHint(Translation.PickedUpBomb);
        plugin.BombObject.transform.ResetTransform();                    
        plugin.BombObject.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        plugin.BombObject.gameObject.transform.parent = ev.Player.GameObject.transform;
        plugin.BombObject.gameObject.transform.localPosition = new Vector3(0, 0.27f, -0.263f);
        plugin.BombObject.gameObject.transform.localRotation = new Quaternion(-0.707106829f, 0, 0, 0.707106829f);

    }

    public void OnDroppedItem(PlayerDroppedItemEventArgs ev)
    {
        if (ev.Pickup.Base.ItemId != Bomb.Base.ItemId) return;
        ev.Throw = false;
        ev.Pickup.Rotation = Quaternion.identity;
        plugin.BombObject.gameObject.transform.parent = ev.Pickup.Transform;
        plugin.BombObject.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        plugin.BombObject.gameObject.transform.localPosition = new Vector3(0, 0, 0);
        plugin.BombObject.gameObject.transform.localRotation = Quaternion.identity;
    }

    public static void OnChangedItemEvent(PlayerChangedItemEventArgs ev)
    {
        Bomb.PersonalCooldownDuration = 0f;
        Bomb.GlobalCooldownDuration = 0f;
        if (ev.OldItem != null && ev.OldItem.Base.ItemId == Bomb.Base.ItemId)
        {
            ev.Player.DisableEffect<Ensnared>();
            ev.Player.DisableEffect<HeavyFooted>();
        }
        if (ev.NewItem != null && ev.NewItem.Base.ItemId != Bomb.Base.ItemId) return;
        ev.Player.SendHint(Translation.EquippedBomb);
    }
}
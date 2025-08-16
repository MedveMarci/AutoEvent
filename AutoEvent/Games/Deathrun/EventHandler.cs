using UnityEngine;
#if EXILED
using Exiled.Events.EventArgs.Player;
#else
using LabApi.Events.Arguments.PlayerEvents;
#endif

namespace AutoEvent.Games.Deathrun;

public class EventHandler
{
#if EXILED
    public void OnSearchingPickup(SearchingPickupEventArgs ev)
#else
    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
#endif
    {
        ev.IsAllowed = false;

        DebugLogger.LogDebug("[Deathrun] click to button");

        // Start the animation when click on the button
        var animator = ev.Pickup.GameObject.GetComponentInParent<Animator>();
        if (animator != null)
        {
            DebugLogger.LogDebug($"[Deathrun] activate animation {animator.name}action");
            animator.Play(animator.name + "action");
        }
    }
}
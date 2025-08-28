using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public abstract class EventHandler
{
    public static void OnPlayerInteractedToy(PlayerInteractedToyEventArgs ev)
    {
        LogManager.Debug("[Deathrun] click to button");

        // Start the animation when click on the button
        var animator = ev.Interactable.GameObject.GetComponentInParent<Animator>();
        if (animator == null) return;
        LogManager.Debug($"[Deathrun] activate animation {animator.name}action");
        animator.Play(animator.name + "action");
    }
}
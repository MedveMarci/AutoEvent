using AutoEvent.ApiFeatures;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public abstract class EventHandler
{
    public static void OnHurt(PlayerHurtEventArgs ev)
    {
        if (ev.DamageHandler is ExplosionDamageHandler explosionDamageHandler) explosionDamageHandler.Damage = 0;
    }

    public static void OnPlayerInteractedToy(PlayerInteractedToyEventArgs ev)
    {
        LogManager.Debug("[Deathrun] Click to button");

        // Start the animation when click on the button
        var animator = ev.Interactable.GameObject.GetComponentInParent<Animator>();
        if (animator == null) return;
        
        var animationName = animator.name + "Action";
        if (animator.name == "Trap1")
        {
            if (ev.Interactable.GameObject.name == "Trap1Interactable1")
                animationName = "Trap1Action1";
            else if (ev.Interactable.GameObject.name == "Trap1Interactable2")
                animationName = "Trap1Action2";
            else if (ev.Interactable.GameObject.name == "Trap1Interactable3") 
                animationName = "Trap1Action3";
        }
        else if (animator.name.Contains("Trap15"))
        {
            if (ev.Interactable.GameObject.name.Contains("Interactable1")) 
                animationName = animator.name + "RowPrimedR";
            else if (ev.Interactable.GameObject.name.Contains("Interactable2"))
                animationName = animator.name + "RowPrimedG";
        }
        

        LogManager.Debug($"[Deathrun] Activate animation {animationName}");
        animator.Play(animationName);
    }
}
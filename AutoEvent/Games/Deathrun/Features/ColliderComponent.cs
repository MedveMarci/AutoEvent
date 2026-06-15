using AutoEvent.API;
using AutoEvent.Interfaces;
using UnityEngine;

namespace AutoEvent.Games.Deathrun;

public class ColliderComponent : MonoBehaviour
{
    private BoxCollider _collider;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider collider)
    {
        var animator = gameObject.GetComponentInParent<Animator>();
        if (animator != null)
        {
            if (EventManager.CurrentEvent is IEventMap eventMap && eventMap.MapInfo.MapName.Contains("temple"))
                animator.Play(animator.name + "action");
            else
                animator.Play(animator.name + "Action");
        }
    }
}
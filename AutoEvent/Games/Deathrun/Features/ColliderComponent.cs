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
        if (animator != null) animator.Play(animator.name + "action");
    }
}
using UnityEngine;

namespace AutoEvent.Games.Spleef;

public class FallPlatformComponent : MonoBehaviour
{
    private BoxCollider _collider;

    private void OnDestroy()
    {
        Destroy(gameObject);
    }
}
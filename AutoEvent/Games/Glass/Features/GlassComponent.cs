using MEC;
using UnityEngine;
#if EXILED
using Exiled.API.Features;
#else
using LabApi.Features.Wrappers;
#endif

namespace AutoEvent.Games.Glass.Features;

public class GlassComponent : MonoBehaviour
{
    private BoxCollider collider;
    public float RegenerationDelay { get; set; } = 5;

    private void Start()
    {
        collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(1, 10, 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Player.Get(other.gameObject) != null)
        {
            gameObject.transform.position += Vector3.down * 5;

            if (RegenerationDelay > 0)
                Timing.CallDelayed(RegenerationDelay, () => { gameObject.transform.position -= Vector3.down * 5; });
        }
    }

    public void Init(float regenerationDelay)
    {
        RegenerationDelay = regenerationDelay;
    }
}
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AutoEvent.Games.Glass.Features;

public class GlassComponent : MonoBehaviour
{
    private BoxCollider _collider;
    public float RegenerationDelay { get; set; } = 5;

    private void Start()
    {
        _collider = gameObject.AddComponent<BoxCollider>();
        _collider.isTrigger = true;
        _collider.size = new Vector3(1, 10, 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Player.Get(other.gameObject) == null) return;
        gameObject.transform.position += Vector3.down * 5;

        if (RegenerationDelay > 0)
            Timing.CallDelayed(RegenerationDelay, () => { gameObject.transform.position -= Vector3.down * 5; });
    }

    public void Init(float regenerationDelay)
    {
        RegenerationDelay = regenerationDelay;
    }
}
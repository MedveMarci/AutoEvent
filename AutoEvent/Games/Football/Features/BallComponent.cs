using UnityEngine;

namespace AutoEvent.Games.Football;

public class BallComponent : MonoBehaviour
{
    private const float BallSpeedBoost = 1f;
    private Rigidbody _rigid;
    private SphereCollider _sphere;

    private void Start()
    {
        _sphere = gameObject.AddComponent<SphereCollider>();
        _sphere.isTrigger = true;
        _sphere.radius = 1.1f;

        _rigid = gameObject.AddComponent<Rigidbody>();
        _rigid.isKinematic = false;
        _rigid.useGravity = true;
        _rigid.mass = 0.1f;
        _rigid.linearDamping = 0.1f;
    }

    private void FixedUpdate()
    {
        transform.position += _rigid.linearVelocity * (Time.fixedDeltaTime * BallSpeedBoost);
    }
}
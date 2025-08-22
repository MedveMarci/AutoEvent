using UnityEngine;

namespace AutoEvent.Games.Jail;

public class DoorComponent : MonoBehaviour
{
    private Transform _doorTransform;
    private bool _isOpen;
    private float _openTime = 2f;

    private void Start()
    {
        _doorTransform = transform;
        _isOpen = false;
    }

    private void Update()
    {
        if (!_isOpen) return;
        if (_openTime <= 0)
        {
            _doorTransform.position += new Vector3(0f, -4f, 0f);
            _isOpen = false;
        }
        else
        {
            _openTime -= Time.deltaTime;
        }
    }

    public void Open()
    {
        _doorTransform.position += new Vector3(0f, 4f, 0f);
        _isOpen = true;
        _openTime = 2f;
    }
}
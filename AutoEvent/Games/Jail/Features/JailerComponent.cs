using UnityEngine;

public class JailerComponent : MonoBehaviour
{
    private Transform _doorTransform;
    private Vector3 _originalPosition;
    public bool IsOpen { get; private set; }

    private void Start()
    {
        _doorTransform = transform;
        _originalPosition = _doorTransform.position;
    }

    public void ToggleDoor()
    {
        IsOpen = !IsOpen;

        if (IsOpen)
            _doorTransform.position += new Vector3(-2.2f, 0, 0);
        else
            _doorTransform.position = _originalPosition;
    }
}
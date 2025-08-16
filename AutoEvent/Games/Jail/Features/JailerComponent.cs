using UnityEngine;

public class JailerComponent : MonoBehaviour
{
    private Transform doorTransform;
    private Vector3 originalPosition;
    public bool IsOpen { get; private set; }

    private void Start()
    {
        doorTransform = transform;
        originalPosition = doorTransform.position;
    }

    public void ToggleDoor()
    {
        IsOpen = !IsOpen;

        if (IsOpen)
            doorTransform.position += new Vector3(-2.2f, 0, 0);
        else
            doorTransform.position = originalPosition;
    }
}
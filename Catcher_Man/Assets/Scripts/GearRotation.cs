using UnityEngine;

public class GearRotation : MonoBehaviour
{
    public enum RotationDirection
    {
        Positive = 1,
        Negative = -1
    }

    [SerializeField, Min(0f)] private float rotationSpeed = 90f;
    [SerializeField] private RotationDirection direction = RotationDirection.Positive;

    private void Update()
    {
        transform.Rotate(
            Vector3.up * ((int)direction * rotationSpeed * Time.deltaTime),
            Space.Self);
    }

    private void OnValidate()
    {
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
    }
}

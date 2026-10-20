using UnityEngine;

public class OwnerFloat : MonoBehaviour
{
    [SerializeField] private float lateralDistance = 0.35f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 1f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }
    private void Update()
    {
        float phase = Time.time * floatSpeed + phaseOffset;
        transform.localPosition = startPosition + Vector3.right * (Mathf.Sin(phase) * lateralDistance)
            + Vector3.up * (Mathf.Sin(phase * 0.7f) * floatHeight);
        transform.localRotation = startRotation;
    }
}

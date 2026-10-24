using UnityEngine;

public class OwnerFloat : MonoBehaviour
{
    [SerializeField] private float lateralDistance = 0.35f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField, Min(0f)] private float reactionHeight = 0.08f;
    [SerializeField, Min(0.1f)] private float reactionDuration = 0.5f;
    private float reactionRemaining;
    public void ReactToBarks() => reactionRemaining = reactionDuration;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private void Start()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }
    private void Update()
    {
        reactionRemaining = Mathf.Max(0f, reactionRemaining - Time.deltaTime);
        float reaction = Mathf.Sin(Mathf.PI * reactionRemaining / reactionDuration) * reactionHeight;
        float phase = Time.time * floatSpeed + phaseOffset;
        transform.localPosition = startPosition + Vector3.right * (Mathf.Sin(phase) * lateralDistance)
            + Vector3.up * (Mathf.Sin(phase * 0.7f) * floatHeight + reaction);
        transform.localRotation = startRotation;
    }
}

using UnityEngine;

public class OwnerFloat : MonoBehaviour
{
    [SerializeField] private float lateralDistance = 0.35f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 1f;
    [Header("Alineación durante la abducción")]
    [SerializeField] private Vector3 journeyCenterOffset = new Vector3(0f, 0.36f, 0.75f);
    [SerializeField, Min(0.05f)] private float journeyAlignmentSeconds = 0.45f;
    [SerializeField, Min(0f)] private float reactionHeight = 0.08f;
    [SerializeField, Min(0.1f)] private float reactionDuration = 0.5f;
    private float reactionRemaining;
    public void ReactToBarks() => reactionRemaining = reactionDuration;
    private OwnersJourney journey;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float journeyAlignment;
    private void Start()
    {
        journey = GetComponentInParent<OwnersJourney>();
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }
    private void Update()
    {
        if (journey && !journey.HasStarted)
        {
            reactionRemaining = 0f;
            journeyAlignment = 0f;
            transform.localPosition = startPosition;
            transform.localRotation = startRotation;
            return;
        }

        reactionRemaining = Mathf.Max(0f, reactionRemaining - Time.deltaTime);
        journeyAlignment = Mathf.MoveTowards(journeyAlignment, 1f,
            Time.deltaTime / Mathf.Max(0.05f, journeyAlignmentSeconds));
        float reaction = Mathf.Sin(Mathf.PI * reactionRemaining / reactionDuration) * reactionHeight;
        float phase = Time.time * floatSpeed + phaseOffset;
        transform.localPosition = startPosition + journeyCenterOffset * journeyAlignment
            + Vector3.right * (Mathf.Sin(phase) * lateralDistance)
            + Vector3.up * (Mathf.Sin(phase * 0.7f) * floatHeight + reaction);
        transform.localRotation = startRotation;
    }
}

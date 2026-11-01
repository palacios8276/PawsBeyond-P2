using UnityEngine;

public sealed class DogVisualAnimator : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerBark bark;
    [SerializeField] private Transform body;
    [SerializeField] private Transform head;
    [SerializeField] private Transform tail;
    [SerializeField] private Transform[] legs;
    [SerializeField, Min(0f)] private float movementThreshold = 0.08f;
    [SerializeField, Min(0f)] private float walkFrequency = 10f;
    [SerializeField, Min(0f)] private float legSwingDegrees = 24f;
    [SerializeField, Min(0f)] private float bodyBob = 0.025f;
    [SerializeField, Min(0f)] private float barkNodDegrees = 13f;

    private Vector3 bodyStart;
    private Quaternion headStart;
    private Quaternion tailStart;
    private Quaternion[] legStarts;
    private int observedBarkCount;
    private float barkReaction;

    private void Awake()
    {
        if (!controller) controller = GetComponentInParent<CharacterController>();
        if (!bark) bark = GetComponentInParent<PlayerBark>();
        bodyStart = body ? body.localPosition : Vector3.zero;
        headStart = head ? head.localRotation : Quaternion.identity;
        tailStart = tail ? tail.localRotation : Quaternion.identity;
        legStarts = new Quaternion[legs == null ? 0 : legs.Length];
        for (int i = 0; i < legStarts.Length; i++)
            legStarts[i] = legs[i] ? legs[i].localRotation : Quaternion.identity;
        observedBarkCount = bark ? bark.BarkCount : 0;
    }

    private void LateUpdate()
    {
        float planarSpeed = controller ? Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude : 0f;
        float moving = planarSpeed > movementThreshold ? 1f : 0f;
        float phase = Time.time * walkFrequency;
        if (body) body.localPosition = bodyStart + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * bodyBob * moving);

        for (int i = 0; i < legStarts.Length; i++)
        {
            if (!legs[i]) continue;
            float direction = (i & 1) == 0 ? 1f : -1f;
            legs[i].localRotation = legStarts[i] * Quaternion.Euler(Mathf.Sin(phase) * legSwingDegrees * direction * moving, 0f, 0f);
        }

        if (bark && bark.BarkCount != observedBarkCount)
        {
            observedBarkCount = bark.BarkCount;
            barkReaction = 1f;
        }
        barkReaction = Mathf.MoveTowards(barkReaction, 0f, Time.deltaTime * 4.5f);
        if (head) head.localRotation = headStart * Quaternion.Euler(-Mathf.Sin(barkReaction * Mathf.PI) * barkNodDegrees, 0f, 0f);
        if (tail) tail.localRotation = tailStart * Quaternion.Euler(0f, Mathf.Sin(Time.time * 8f) * (12f + moving * 18f), 0f);
    }
}

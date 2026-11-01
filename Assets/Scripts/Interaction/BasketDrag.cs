using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasketDrag : MonoBehaviour
{
    public enum DragState { Free, Extracting, Held, Releasing }

    [Header("Recogida hacia el hocico")]
    [SerializeField, Min(0.05f)] private float extractionDuration = 0.28f;
    [SerializeField, Min(0f)] private float extractionHeight = 0.45f;
    [SerializeField, Range(0.1f, 0.9f)] private float liftPortion = 0.55f;
    [Header("Sujeción")]
    [SerializeField] private Vector3 heldOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField] private bool preserveGrabRotation = true;
    [SerializeField] private Vector3 heldEulerOffset;
    [SerializeField, Min(0f)] private float obstaclePadding = 0.025f;

    private Rigidbody body;
    private Transform holder;
    private Collider[] playerColliders;
    private Collider[] basketColliders;
    private Vector3 extractionStart, extractionPeak;
    private Quaternion extractionStartRotation, heldRotationOffset;
    private float extractionElapsed;
    private bool previousDetectCollisions;
    private CollisionDetectionMode previousCollisionMode;
    private RigidbodyInterpolation previousInterpolation;
    private InteractableHighlight highlight;
    private DragState state;
    public DragState State => state;
    public bool IsHeld => state == DragState.Extracting || state == DragState.Held;
    public bool IsExtracting => state == DragState.Extracting;
    public bool CanBeGrabbed => body && !body.isKinematic && state == DragState.Free;
    public bool HasBeenDragged { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        basketColliders = GetComponentsInChildren<Collider>();
        highlight = GetComponent<InteractableHighlight>();
        if (!highlight) highlight = gameObject.AddComponent<InteractableHighlight>();
    }

    public bool Grab(Transform player)
    {
        return Grab(player, null);
    }

    public bool Grab(Transform player, Transform grip)
    {
        if (body.isKinematic || state != DragState.Free) return false;
        previousDetectCollisions = body.detectCollisions;
        previousCollisionMode = body.collisionDetectionMode;
        previousInterpolation = body.interpolation;

        // Pulling frees clothes snagged in the doorway without changing puzzle rules.
        body.constraints &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);
        holder = grip ? grip : player;
        playerColliders = player.GetComponentsInChildren<Collider>();
        SetPlayerCollisions(true);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.detectCollisions = false;
        body.isKinematic = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        extractionStart = body.position;
        extractionPeak = extractionStart + Vector3.up * extractionHeight;
        extractionStartRotation = body.rotation;
        heldRotationOffset = preserveGrabRotation
            ? Quaternion.Inverse(holder.rotation) * body.rotation
            : Quaternion.Euler(heldEulerOffset);
        extractionElapsed = 0f;
        state = DragState.Extracting;
        HasBeenDragged = true;
        if (highlight) highlight.SetHighlighted(false);
        return true;
    }

    public void SetInteractionHighlight(bool active)
    {
        if (highlight) highlight.SetHighlighted(active && CanBeGrabbed);
    }

    public void Release()
    {
        if (!IsHeld) return;
        state = DragState.Releasing;
        SetPlayerCollisions(false);
        holder = null;
        if (body)
        {
            // Todo BasketDrag recogible vuelve a ser un cuerpo libre. Restaurar constraints
            // previos dejaba prendas verticales y juguetes suspendidos tras soltarlos.
            body.detectCollisions = true;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
            body.collisionDetectionMode = previousCollisionMode;
            body.interpolation = previousInterpolation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
        playerColliders = null;
        state = DragState.Free;
    }

    private void SetPlayerCollisions(bool ignore)
    {
        if (playerColliders == null || basketColliders == null) return;
        foreach (var a in basketColliders)
            foreach (var b in playerColliders)
                if (a && b) Physics.IgnoreCollision(a, b, ignore);
    }

    private void FixedUpdate()
    {
        if (!holder || !IsHeld) return;
        Vector3 target = holder.TransformPoint(heldOffset);
        Quaternion targetRotation = holder.rotation * heldRotationOffset;
        if (state == DragState.Extracting)
        {
            extractionElapsed += Time.fixedDeltaTime;
            float normalized = Mathf.Clamp01(extractionElapsed / extractionDuration);
            float liftEnd = Mathf.Clamp(liftPortion, 0.1f, 0.9f);
            bool lifting = normalized < liftEnd;
            Vector3 next = lifting
                ? Vector3.Lerp(extractionStart, extractionPeak, Mathf.SmoothStep(0f, 1f, normalized / liftEnd))
                : Vector3.Lerp(extractionPeak, target,
                    Mathf.SmoothStep(0f, 1f, (normalized - liftEnd) / (1f - liftEnd)));
            if (!lifting)
            {
                body.detectCollisions = previousDetectCollisions;
                next = GetObstacleLimitedPosition(next);
            }
            body.MovePosition(next);
            body.MoveRotation(Quaternion.Slerp(extractionStartRotation, targetRotation,
                Mathf.SmoothStep(0f, 1f, normalized)));
            if (normalized < 1f) return;
            state = DragState.Held;
            body.detectCollisions = previousDetectCollisions;
        }

        body.MovePosition(GetObstacleLimitedPosition(target));
        body.MoveRotation(targetRotation);
    }

    private Vector3 GetObstacleLimitedPosition(Vector3 target)
    {
        Vector3 delta = target - body.position;
        float distance = delta.magnitude;
        if (distance > 0.0001f && body.SweepTest(delta / distance, out RaycastHit hit,
            distance, QueryTriggerInteraction.Ignore))
            return body.position + delta.normalized * Mathf.Max(0f, hit.distance - obstaclePadding);
        return target;
    }

    private void OnDisable() => Release();
}

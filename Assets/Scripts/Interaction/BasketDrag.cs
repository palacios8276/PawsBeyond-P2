using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasketDrag : MonoBehaviour
{
    public enum DragState { Free, Extracting, Held, Releasing }

    [Header("Recogida hacia el hocico")]
    [SerializeField, Min(0.05f)] private float extractionDuration = 0.28f;
    [SerializeField, Min(0f)] private float extractionHeight = 0.45f;
    [SerializeField, Range(0.1f, 0.9f)] private float liftPortion = 0.55f;
    [SerializeField, Range(0.05f, 0.4f)] private float descentPortion = 0.25f;
    [SerializeField, Min(0f)] private float extractionClearanceDistance;
    [Header("Sujeción")]
    [SerializeField] private Vector3 heldOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField] private bool preserveGrabRotation = true;
    [SerializeField] private Vector3 heldEulerOffset;
    [SerializeField, Min(0f)] private float obstaclePadding = 0.025f;
    [Header("Liberación física")]
    [SerializeField] private RigidbodyConstraints freeConstraints = RigidbodyConstraints.None;
    [SerializeField, Min(0f)] private float releaseDownwardSpeed = 0.25f;
    [SerializeField, Min(0f)] private float releaseCollisionDelay = 0.12f;
    [SerializeField, Min(0.05f)] private float maximumCollisionDelay = 0.5f;
    [SerializeField, Min(0f)] private float releaseSeparationPadding = 0.015f;

    private Rigidbody body;
    private Transform holder;
    private Collider[] playerColliders;
    private Collider[] basketColliders;
    private Vector3 extractionStart, extractionPeak;
    private Quaternion extractionStartRotation, heldRotationOffset;
    private float extractionElapsed;
    private bool clearancePending;
    private float restorePlayerCollisionsAt, forceRestorePlayerCollisionsAt;
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
        clearancePending = extractionClearanceDistance > 0f;
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
        holder = null;
        if (body)
        {
            body.detectCollisions = true;
            body.useGravity = true;
            body.constraints = freeConstraints;
            body.collisionDetectionMode = previousCollisionMode;
            body.interpolation = previousInterpolation;
            ResolveReleaseOverlaps();
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (releaseDownwardSpeed > 0f)
                body.linearVelocity = Vector3.down * releaseDownwardSpeed;
            body.WakeUp();
        }
        restorePlayerCollisionsAt = Time.fixedTime + releaseCollisionDelay;
        forceRestorePlayerCollisionsAt = Time.fixedTime + Mathf.Max(releaseCollisionDelay,
            maximumCollisionDelay);
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
        if (state == DragState.Releasing)
        {
            bool delayElapsed = Time.fixedTime >= restorePlayerCollisionsAt;
            bool forced = Time.fixedTime >= forceRestorePlayerCollisionsAt;
            if (delayElapsed && (!OverlapsPlayer() || forced)) CompleteRelease();
            return;
        }
        if (!holder || !IsHeld) return;
        Vector3 target = holder.TransformPoint(heldOffset);
        Quaternion targetRotation = holder.rotation * heldRotationOffset;
        if (state == DragState.Extracting)
        {
            extractionElapsed += Time.fixedDeltaTime;
            float normalized = Mathf.Clamp01(extractionElapsed / extractionDuration);
            float liftEnd = Mathf.Clamp(liftPortion, 0.1f, 0.9f);
            float descentStart = Mathf.Clamp(1f - descentPortion, liftEnd + 0.05f, 0.95f);
            Vector3 targetClearance = new Vector3(target.x,
                Mathf.Max(extractionPeak.y, target.y + obstaclePadding), target.z);
            Vector3 next;
            if (normalized < liftEnd)
                next = Vector3.Lerp(extractionStart, extractionPeak,
                    Mathf.SmoothStep(0f, 1f, normalized / liftEnd));
            else if (normalized < descentStart)
                next = Vector3.Lerp(extractionPeak, targetClearance,
                    Mathf.SmoothStep(0f, 1f, (normalized - liftEnd) / (descentStart - liftEnd)));
            else
            {
                body.detectCollisions = previousDetectCollisions;
                Vector3 extractionTarget = clearancePending ? targetClearance : target;
                next = Vector3.Lerp(targetClearance, extractionTarget,
                    Mathf.SmoothStep(0f, 1f, (normalized - descentStart) / (1f - descentStart)));
                next = GetObstacleLimitedPosition(next);
            }
            body.MovePosition(next);
            body.MoveRotation(Quaternion.Slerp(extractionStartRotation, targetRotation,
                Mathf.SmoothStep(0f, 1f, normalized)));
            if (normalized < 1f) return;
            state = DragState.Held;
            body.detectCollisions = previousDetectCollisions;
        }

        if (clearancePending)
        {
            Vector2 start = new Vector2(extractionStart.x, extractionStart.z);
            Vector2 requested = new Vector2(target.x, target.z);
            if (Vector2.Distance(start, requested) >= extractionClearanceDistance)
                clearancePending = false;
            else
                target.y = Mathf.Max(target.y, extractionPeak.y);
        }
        body.MovePosition(GetObstacleLimitedPosition(target));
        body.MoveRotation(targetRotation);
    }

    private void ResolveReleaseOverlaps()
    {
        if (basketColliders == null) return;
        Physics.SyncTransforms();
        for (int iteration = 0; iteration < 3; iteration++)
        {
            Vector3 correction = Vector3.zero;
            foreach (Collider own in basketColliders)
            {
                if (!own || !own.enabled || own.isTrigger) continue;
                Collider[] nearby = Physics.OverlapBox(own.bounds.center,
                    own.bounds.extents + Vector3.one * releaseSeparationPadding,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                foreach (Collider other in nearby)
                {
                    if (!other || other.isTrigger || other.transform.IsChildOf(transform) ||
                        IsPlayerCollider(other)) continue;
                    if (Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                        other, other.transform.position, other.transform.rotation,
                        out Vector3 direction, out float distance) && distance > 0.001f)
                        correction += direction * (distance + releaseSeparationPadding);
                }
            }
            if (correction.sqrMagnitude < 0.000001f) break;
            body.position += Vector3.ClampMagnitude(correction, 0.25f);
            Physics.SyncTransforms();
        }
    }

    private bool OverlapsPlayer()
    {
        if (playerColliders == null || basketColliders == null) return false;
        foreach (Collider own in basketColliders)
            foreach (Collider playerCollider in playerColliders)
                if (own && own.enabled && playerCollider && playerCollider.enabled &&
                    Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                        playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
                        out _, out float distance) && distance > 0.001f)
                    return true;
        return false;
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (playerColliders == null) return false;
        foreach (Collider playerCollider in playerColliders)
            if (playerCollider == candidate) return true;
        return false;
    }

    private void CompleteRelease()
    {
        SetPlayerCollisions(false);
        playerColliders = null;
        state = DragState.Free;
        if (body) body.WakeUp();
    }

    private Vector3 GetObstacleLimitedPosition(Vector3 target)
    {
        Vector3 delta = target - body.position;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return target;
        float nearestObstacle = distance;
        RaycastHit[] hits = body.SweepTestAll(delta / distance, distance,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider || hit.collider.transform.IsChildOf(transform) ||
                IsPlayerCollider(hit.collider)) continue;
            nearestObstacle = Mathf.Min(nearestObstacle, hit.distance);
        }
        if (nearestObstacle < distance)
            return body.position + delta.normalized * Mathf.Max(0f, nearestObstacle - obstaclePadding);
        return target;
    }

    private void OnDisable()
    {
        if (IsHeld) Release();
        if (state == DragState.Releasing) CompleteRelease();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
[Serializable] public sealed class DragObjectEvent : UnityEvent<BasketDrag> { }

public class PlayerInteraction : MonoBehaviour
{
    [Header("Zona frontal del hocico")]
    [SerializeField, FormerlySerializedAs("interactionDistance"), Min(0.1f)] private float interactionReach = 1f;
    [SerializeField, Min(0.05f)] private float interactionRadius = 0.55f;
    [SerializeField, Range(10f, 160f)] private float interactionAngle = 100f;
    [SerializeField] private LayerMask interactableMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private Transform interactionOrigin;
    private PlayerInputActions inputActions;
    private BasketDrag heldBasket;
    private readonly Collider[] candidates = new Collider[32];
    private readonly HashSet<BasketDrag> inspected = new HashSet<BasketDrag>();
    [SerializeField] private DragObjectEvent onObjectGrabbed = new DragObjectEvent();
    [SerializeField] private DragObjectEvent onObjectReleased = new DragObjectEvent();
    public DragObjectEvent OnObjectGrabbed => onObjectGrabbed;
    public DragObjectEvent OnObjectReleased => onObjectReleased;
    public bool IsDragging => heldBasket && heldBasket.IsHeld;
    public BasketDrag LastCandidate { get; private set; }

    private void Awake() => inputActions = new PlayerInputActions();

    private void OnEnable()
    {
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Disable();
        if (heldBasket) heldBasket.Release();
        heldBasket = null;
    }

    private void OnDestroy() => inputActions?.Dispose();
    private void OnInteract(InputAction.CallbackContext context) => TryInteract();

    private void TryInteract()
    {
        if (IsDragging)
        {
            var released = heldBasket;
            heldBasket.Release();
            heldBasket = null;
            onObjectReleased.Invoke(released);
            return;
        }
        BasketDrag candidate = FindClosestCandidate();
        if (candidate && candidate.Grab(transform, interactionOrigin))
        {
            heldBasket = candidate;
            onObjectGrabbed.Invoke(candidate);
        }
    }

    private BasketDrag FindClosestCandidate()
    {
        LastCandidate = null;
        if (!interactionOrigin) return null;

        Vector3 origin = interactionOrigin.position;
        Vector3 forward = interactionOrigin.forward.normalized;
        Vector3 center = origin + forward * (interactionReach * 0.5f);
        float searchRadius = Mathf.Sqrt(interactionRadius * interactionRadius +
            interactionReach * interactionReach * 0.25f);
        int count = Physics.OverlapSphereNonAlloc(center, searchRadius, candidates,
            interactableMask, QueryTriggerInteraction.Ignore);
        inspected.Clear();
        float closestDistance = float.PositiveInfinity;
        float minimumDot = Mathf.Cos(interactionAngle * 0.5f * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            Collider candidateCollider = candidates[i];
            if (!candidateCollider) continue;
            BasketDrag candidate = candidateCollider.GetComponentInParent<BasketDrag>();
            if (!candidate || candidate.IsHeld || !inspected.Add(candidate)) continue;

            Vector3 point = candidateCollider.ClosestPoint(origin);
            Vector3 delta = point - origin;
            if (delta.sqrMagnitude < 0.0001f) delta = candidateCollider.bounds.center - origin;
            float forwardDistance = Vector3.Dot(delta, forward);
            if (forwardDistance < 0f || forwardDistance > interactionReach) continue;
            Vector3 lateral = delta - forward * forwardDistance;
            if (lateral.sqrMagnitude > interactionRadius * interactionRadius) continue;
            if (delta.sqrMagnitude > 0.0001f && Vector3.Dot(delta.normalized, forward) < minimumDot) continue;
            if (IsOccluded(candidate, point)) continue;

            float distance = delta.sqrMagnitude;
            if (distance >= closestDistance) continue;
            closestDistance = distance;
            LastCandidate = candidate;
        }

        return LastCandidate;
    }

    private bool IsOccluded(BasketDrag candidate, Vector3 point)
    {
        Vector3 origin = interactionOrigin.position;
        Vector3 delta = point - origin;
        float distance = delta.magnitude;
        if (distance < 0.001f) return false;
        if (!Physics.Raycast(origin, delta / distance, out RaycastHit hit, distance + 0.02f,
            obstacleMask, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.GetComponentInParent<BasketDrag>() != candidate;
    }

    private void OnDrawGizmosSelected()
    {
        if (!interactionOrigin) return;
        Vector3 origin = interactionOrigin.position;
        Vector3 forward = interactionOrigin.forward.normalized;
        Vector3 center = origin + forward * (interactionReach * 0.5f);
        float searchRadius = Mathf.Sqrt(interactionRadius * interactionRadius +
            interactionReach * interactionReach * 0.25f);
        Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.75f);
        Gizmos.DrawWireSphere(center, searchRadius);
        Gizmos.DrawLine(origin, origin + forward * interactionReach);
        Vector3 right = interactionOrigin.right * interactionRadius;
        Gizmos.DrawLine(origin, origin + forward * interactionReach + right);
        Gizmos.DrawLine(origin, origin + forward * interactionReach - right);
    }
}

using UnityEngine;
using System.Collections.Generic;

public class DoorTriggerDetector : MonoBehaviour
{
    [SerializeField] private HingeJoint doorHinge;
    [SerializeField] private float openTarget = 90f;
    [SerializeField] private float closedTarget = 0f;
    [SerializeField, Min(0f)] private float closingDelay = 0.6f;
    [Header("Apertura bidireccional opcional")]
    [SerializeField] private bool bidirectionalOpening;
    [SerializeField, Range(45f, 90f)] private float bidirectionalAngle = 78f;
    [SerializeField, Min(0.01f)] private float sideDeadZone = 0.15f;
    private float closeAfter;
    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private bool closingForPuzzle;
    private bool narrativeOpeningRequested;
    [SerializeField] private CharacterController allowedPlayer;
    private RigidbodyConstraints unlockedConstraints;
    private float lastTarget = float.NaN;
    private float selectedOpenTarget;
    private bool hasSelectedDirection;
    public bool IsPuzzleLocked { get; private set; }

    // Solicitud explícita del recorrido; nunca se activa por colliders de otros actores.
    public void RequestNarrativeOpening(bool requested)
    {
        if (narrativeOpeningRequested == requested) return;
        narrativeOpeningRequested = requested;
        if (requested) closeAfter = 0f;
        else if (occupants.Count == 0) closeAfter = Time.time + closingDelay;
        RefreshTarget();
    }

    public void RequestNarrativeOpening(bool requested, Vector3 actorPosition)
    {
        if (requested) SelectOpeningDirection(actorPosition);
        RequestNarrativeOpening(requested);
    }

    private void RefreshTarget()
    {
        SetTarget(closingForPuzzle || IsPuzzleLocked || (occupants.Count == 0 && !narrativeOpeningRequested && Time.time >= closeAfter)
            ? closedTarget : CurrentOpenTarget());
    }

    public void CloseForPuzzle(bool closing)
    {
        if (!closing && IsPuzzleLocked)
        {
            doorHinge.GetComponent<Rigidbody>().constraints = unlockedConstraints;
            IsPuzzleLocked = false;
        }
        if (closing) closeAfter = 0f;
        closingForPuzzle = closing;
        RefreshTarget();
    }

    // Conserva Rigidbody y bisagra: inmoviliza el acceso cerrado, no al jugador.
    public void LockForPuzzle()
    {
        if (!doorHinge || IsPuzzleLocked) return;
        var body = doorHinge.GetComponent<Rigidbody>();
        unlockedConstraints = body.constraints;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeAll;
        IsPuzzleLocked = true;
    }

    private void Start()
    {
        if (!doorHinge)
        {
            Debug.LogError("Door trigger: missing hinge.", this);
            enabled = false;
            return;
        }
        Rigidbody body = doorHinge.GetComponent<Rigidbody>();
        if (bidirectionalOpening)
        {
            body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            unlockedConstraints = body.constraints;
            JointLimits limits = doorHinge.limits;
            limits.min = -bidirectionalAngle;
            limits.max = bidirectionalAngle;
            doorHinge.limits = limits;
            doorHinge.useLimits = true;
        }
        selectedOpenTarget = openTarget;
        SetTarget(closedTarget);
    }

    private void SetTarget(float target)
    {
        if (!doorHinge || Mathf.Approximately(lastTarget, target)) return;
        lastTarget = target;
        var spring = doorHinge.spring;
        spring.targetPosition = target;
        doorHinge.spring = spring;
        doorHinge.useSpring = true;
        doorHinge.GetComponent<Rigidbody>().WakeUp();
    }

    private void Update()
    {
        if (closeAfter <= 0f || Time.time < closeAfter) return;
        closeAfter = 0f;
        RefreshTarget();
    }

    private void OnTriggerEnter(Collider other) => TrackPlayer(other);
    private void OnTriggerStay(Collider other) => TrackPlayer(other);

    private void TrackPlayer(Collider other)
    {
        if (!allowedPlayer || other != allowedPlayer) return;
        if (occupants.Add(other))
        {
            SelectOpeningDirection(allowedPlayer.transform.position);
            closeAfter = 0f;
            RefreshTarget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!occupants.Remove(other)) return;
        if (occupants.Count == 0) closeAfter = Time.time + closingDelay;
        RefreshTarget();
    }

    private float CurrentOpenTarget() => bidirectionalOpening && hasSelectedDirection
        ? selectedOpenTarget : openTarget;

    private void SelectOpeningDirection(Vector3 actorPosition)
    {
        if (!bidirectionalOpening || !doorHinge) return;
        if (hasSelectedDirection && Mathf.Abs(doorHinge.angle) > 10f) return;
        Transform door = doorHinge.transform;
        Vector3 axis = door.TransformDirection(doorHinge.axis).normalized;
        Vector3 hingePoint = door.TransformPoint(doorHinge.anchor);
        Vector3 radial = Vector3.ProjectOnPlane(doorHinge.GetComponent<Rigidbody>().worldCenterOfMass - hingePoint, axis);
        if (radial.sqrMagnitude < 0.001f) return;
        Vector3 positiveSwing = Vector3.Cross(axis, radial.normalized);
        float side = Vector3.Dot(actorPosition - hingePoint, positiveSwing);
        if (Mathf.Abs(side) < sideDeadZone && hasSelectedDirection) return;
        float direction = Mathf.Abs(side) < sideDeadZone ? Mathf.Sign(openTarget) : -Mathf.Sign(side);
        selectedOpenTarget = direction * bidirectionalAngle;
        hasSelectedDirection = true;
    }
}

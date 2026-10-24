using UnityEngine;
using System.Collections.Generic;

public class DoorTriggerDetector : MonoBehaviour
{
    [SerializeField] private HingeJoint doorHinge;
    [SerializeField] private float openTarget = 90f;
    [SerializeField] private float closedTarget = 0f;
    [SerializeField, Min(0f)] private float closingDelay = 0.6f;
    private float closeAfter;
    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private bool closingForPuzzle;
    private bool narrativePresence;
    private RigidbodyConstraints unlockedConstraints;
    private float lastTarget = float.NaN;
    public bool IsPuzzleLocked { get; private set; }

    public void SetNarrativePresence(bool present)
    {
        if (narrativePresence == present) return;
        if (!present && occupants.Count == 0) closeAfter = Time.time + closingDelay;
        narrativePresence = present;
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        SetTarget(closingForPuzzle || (occupants.Count == 0 && !narrativePresence && Time.time >= closeAfter)
            ? closedTarget : openTarget);
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
        if (!other.GetComponentInParent<CharacterController>() || !other.transform.root.CompareTag("Player")) return;
        if (occupants.Add(other)) { closeAfter = 0f; RefreshTarget(); }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!occupants.Remove(other)) return;
        if (occupants.Count == 0) closeAfter = Time.time + closingDelay;
        RefreshTarget();
    }
}

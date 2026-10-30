using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public sealed class JourneyWaypointEvent : UnityEvent<int> { }

public class OwnersJourney : MonoBehaviour
{
    public event System.Action<int> WaypointReached;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Renderer[] owners;
    [SerializeField] private Camera viewingCamera;
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.25f;
    [SerializeField, Min(1f)] private float postBedroomSpeedMultiplier = 1.2f;
    [SerializeField] private float observationTime = 1.25f;
    [SerializeField] private float departureDelay = 1.5f;
    [SerializeField] private int bedroomCrossing = 2;
    [SerializeField] private int returnBedroomCrossing = -1;
    [SerializeField] private int bathroomCrossing = 8;
    [SerializeField] private int bathroomStop = 9;
    [SerializeField] private int bathroomExit = 11;
    [SerializeField] private DoorTriggerDetector bedroomTrigger;
    [SerializeField] private HingeJoint bedroomDoor;
    [SerializeField] private DoorTriggerDetector bathroomTrigger;
    [SerializeField] private HingeJoint bathroomDoor;
    [SerializeField] private BathroomBasketDrop bathroomPuzzle;
    [SerializeField] private UnityEvent onJourneyStarted = new UnityEvent();
    [SerializeField] private bool automaticStart = true;
    [SerializeField, Min(0.1f)] private float maximumStartWait = 3f;
    [SerializeField] private JourneyWaypointEvent onWaypointReached = new JourneyWaypointEvent();
    [SerializeField] private UnityEvent onBathroomEntered = new UnityEvent();
    [SerializeField] private UnityEvent onBathroomExited = new UnityEvent();
    private bool bedroomSpeedIncreased, bathroomEntered;
    public float CurrentMoveSpeed => moveSpeed * (bedroomSpeedIncreased ? postBedroomSpeedMultiplier : 1f);
    public void ApplyBedroomSpeedIncrease() => bedroomSpeedIncreased = true;
    private bool narrativeHeld;
    public void SetNarrativeHold(bool held) => narrativeHeld = held;
    private bool playerVisited;
    private float startWait;
    private bool playerPresent;
    private float observed;
    private float delay;
    private int next;
    public bool HasStarted { get; private set; }
    public bool IsComplete => HasStarted && next >= waypoints.Length;
    public int CurrentWaypoint => next;
    public int LastReachedWaypoint { get; private set; } = -1;
    public bool HasReachedWaypoint(int index) => LastReachedWaypoint >= index;
    public bool WaitingForBathroom => HasStarted && !bathroomPuzzle.IsSolved
        && ((next == bathroomStop && Vector3.Distance(transform.position, waypoints[next].position) < 0.02f)
            || (next == bathroomCrossing && bathroomPuzzle.IsActivated));

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() && other.transform.root.CompareTag("Player"))
        { playerPresent = true; playerVisited = true; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStarted && other.GetComponentInParent<CharacterController>() && other.transform.root.CompareTag("Player"))
            playerPresent = false;
    }

    // Puede llamarse desde el futuro tutorial con automaticStart desactivado.
    public void BeginJourney()
    {
        if (HasStarted || !enabled) return;
        HasStarted = true;
        delay = departureDelay;
        GetComponent<BoxCollider>().enabled = false;
        onJourneyStarted.Invoke();
    }

    private bool OwnersInView()
    {
        foreach (var owner in owners)
        {
            Vector3 point = viewingCamera.WorldToViewportPoint(owner.bounds.center);
            if (point.z <= 0f || point.x < 0.05f || point.x > 0.95f || point.y < 0.05f || point.y > 0.95f) continue;
            if (!Physics.Linecast(viewingCamera.transform.position, owner.bounds.center,
                out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider.CompareTag("Player")) return true;
        }
        return false;
    }

    private void Update()
    {
        if (!HasStarted)
        {
            if (!automaticStart || !playerVisited) return;
            startWait += Time.deltaTime;
            if (playerPresent && OwnersInView()) observed += Time.deltaTime;
            if (observed < observationTime && startWait < maximumStartWait) return;
            BeginJourney();
        }
        if (delay > 0f) { delay -= Time.deltaTime; return; }
        if (narrativeHeld) return;
        if (IsComplete) { bedroomTrigger.RequestNarrativeOpening(false); return; }
        bool returningBedroom = returnBedroomCrossing >= 1 && next >= returnBedroomCrossing - 1 && next <= returnBedroomCrossing + 1;
        bool crossingBedroom = next == bedroomCrossing || returningBedroom && next <= returnBedroomCrossing;
        bedroomTrigger.RequestNarrativeOpening(next == bedroomCrossing || returningBedroom);
        bool bathroomBlocked = next == bathroomCrossing && bathroomPuzzle.IsActivated && !bathroomPuzzle.IsSolved;
        bathroomTrigger.RequestNarrativeOpening((next == bathroomCrossing || next == bathroomExit) && !bathroomBlocked);
        if (crossingBedroom)
        {
            if (Mathf.Abs(bedroomDoor.angle) < 75f || returningBedroom && float.IsNaN(bedroomDoor.angle)) return;
        }
        if (next == bathroomCrossing || next == bathroomExit)
        {
            // If the player arrived early, wait outside instead of crossing a blocked door.
            if (bathroomBlocked)
                return;
            if (Mathf.Abs(bathroomDoor.angle) < 75f) return;
        }
        transform.position = Vector3.MoveTowards(transform.position, waypoints[next].position, CurrentMoveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, waypoints[next].position) > 0.01f) return;
        if (next == bathroomStop)
        {
            if (!bathroomEntered) { bathroomEntered = true; onBathroomEntered.Invoke(); }
            bathroomPuzzle.NotifyOwnersArrived();
            if (!bathroomPuzzle.IsSolved) return;
        }
        if (next == bathroomExit && bathroomEntered) onBathroomExited.Invoke();
        LastReachedWaypoint = next;
        onWaypointReached.Invoke(next);
        WaypointReached?.Invoke(next);
        next++;
    }

    // Wired to the existing puzzle UnityEvent, without changing puzzle logic.
    public void ResumeAfterBathroom()
    {
        if (bathroomPuzzle.IsSolved && next == bathroomStop) next++;
    }
    private void OnDisable()
    {
        if (bedroomTrigger) bedroomTrigger.RequestNarrativeOpening(false);
        if (bathroomTrigger) bathroomTrigger.RequestNarrativeOpening(false);
    }
}

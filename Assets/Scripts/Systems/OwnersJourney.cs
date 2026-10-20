using UnityEngine;
using UnityEngine.Events;

public class OwnersJourney : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Renderer[] owners;
    [SerializeField] private Camera viewingCamera;
    [SerializeField] private float moveSpeed = 1.1f;
    [SerializeField] private float observationTime = 1.25f;
    [SerializeField] private float departureDelay = 1.5f;
    [SerializeField] private int bedroomCrossing = 2;
    [SerializeField] private int bathroomCrossing = 8;
    [SerializeField] private int bathroomStop = 9;
    [SerializeField] private int bathroomExit = 11;
    [SerializeField] private DoorTriggerDetector bedroomTrigger;
    [SerializeField] private HingeJoint bedroomDoor;
    [SerializeField] private DoorTriggerDetector bathroomTrigger;
    [SerializeField] private HingeJoint bathroomDoor;
    [SerializeField] private BathroomBasketDrop bathroomPuzzle;
    [SerializeField] private UnityEvent onJourneyStarted = new UnityEvent();
    private bool playerPresent;
    private float observed;
    private float delay;
    private int next;
    public bool HasStarted { get; private set; }
    public bool IsComplete => HasStarted && next >= waypoints.Length;
    public int CurrentWaypoint => next;
    public bool WaitingForBathroom => HasStarted && !bathroomPuzzle.IsSolved
        && ((next == bathroomStop && Vector3.Distance(transform.position, waypoints[next].position) < 0.02f)
            || (next == bathroomCrossing && bathroomPuzzle.IsActivated));

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() && other.transform.root.CompareTag("Player"))
            playerPresent = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasStarted && other.GetComponentInParent<CharacterController>() && other.transform.root.CompareTag("Player"))
            playerPresent = false;
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
            observed = playerPresent && OwnersInView() ? observed + Time.deltaTime : 0f;
            if (observed < observationTime) return;
            HasStarted = true;
            delay = departureDelay;
            GetComponent<BoxCollider>().enabled = false;
            onJourneyStarted.Invoke();
        }
        if (delay > 0f) { delay -= Time.deltaTime; return; }
        if (IsComplete) return;
        if (next > bedroomCrossing) bedroomTrigger.SetNarrativePresence(false);
        if (next == bedroomCrossing)
        {
            bedroomTrigger.SetNarrativePresence(true);
            if (Mathf.Abs(bedroomDoor.angle) < 75f) return;
        }
        if (next > bathroomCrossing && next < bathroomExit) bathroomTrigger.SetNarrativePresence(false);
        if (next == bathroomCrossing || next == bathroomExit)
        {
            // If the player arrived early, wait outside instead of crossing a blocked door.
            if (next == bathroomCrossing && bathroomPuzzle.IsActivated && !bathroomPuzzle.IsSolved)
            { bathroomTrigger.SetNarrativePresence(false); return; }
            bathroomTrigger.SetNarrativePresence(true);
            if (Mathf.Abs(bathroomDoor.angle) < 75f) return;
        }
        if (next > bathroomExit) bathroomTrigger.SetNarrativePresence(false);
        transform.position = Vector3.MoveTowards(transform.position, waypoints[next].position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, waypoints[next].position) > 0.01f) return;
        if (next == bathroomStop && !bathroomPuzzle.IsSolved) return;
        next++;
    }

    // Wired to the existing puzzle UnityEvent, without changing puzzle logic.
    public void ResumeAfterBathroom()
    {
        if (bathroomPuzzle.IsSolved && next == bathroomStop) next++;
    }

    private void OnDisable()
    {
        if (bedroomTrigger) bedroomTrigger.SetNarrativePresence(false);
        if (bathroomTrigger) bathroomTrigger.SetNarrativePresence(false);
    }
}

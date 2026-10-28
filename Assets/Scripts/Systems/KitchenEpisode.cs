using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum KitchenEpisodeStep { Dormant, FirstEncounter, FirstEscape, SecondEncounter, FirstRun, Repairing, FinalEscape, Complete }

// Coordinación local de cocina; los episodios anteriores conservan sus autoridades.
public sealed class KitchenEpisode : MonoBehaviour
{
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private BathroomBasketDrop bathroom;
    [SerializeField] private PlayerBark playerBark;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private KitchenAlien firstAlien, secondAlien;
    [SerializeField] private KitchenShutter firstShutter, secondShutter;
    [SerializeField] private Rigidbody toyPrefab;
    [SerializeField] private Transform toyLanding;
    [SerializeField] private Transform[] firstContactRoute, firstExitRoute, repairRoute, secondContactRoute, secondExitRoute;
    [SerializeField] private ParticleSystem repairParticles;
    [SerializeField] private int arrivalWaypoint = 19;
    [SerializeField, Min(0.1f)] private float hearingRange = 4.5f;
    [SerializeField, Range(0f, 90f)] private float hearingAngle = 65f;
    [SerializeField] private bool secondEncounterReady;
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onHeardBark = new UnityEvent();
    [SerializeField] private UnityEvent onFirstSealed = new UnityEvent();
    [SerializeField] private UnityEvent onRepairStarted = new UnityEvent();
    [SerializeField] private UnityEvent onRepairInterrupted = new UnityEvent();
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();
    private bool arrived;
    private int consumedBark = -1;
    private bool ownsBarkFocus;
    public KitchenEpisodeStep Step { get; private set; }
    public int AcceptedInterventions { get; private set; }
    public int HeardBarks { get; private set; }
    public int CompletionCount { get; private set; }
    public KitchenAlien ActiveAlien => Step == KitchenEpisodeStep.FirstEncounter ? firstAlien
        : Step == KitchenEpisodeStep.SecondEncounter || Step == KitchenEpisodeStep.Repairing ? secondAlien : null;

    private void OnEnable()
    {
        if (beam) beam.InterferenceAccepted.AddListener(OnInterferenceAccepted);
    }

    public void OnWaypointReached(int index)
    {
        if (arrived || index != arrivalWaypoint || !bathroom.IsSolved) return;
        arrived = true;
        progression.CompleteBathroom();
        if (progression.CurrentEpisode == NarrativeEpisode.Kitchen) journey.SetNarrativeHold(true);
    }

    private void Update()
    {
        if (!arrived || Step != KitchenEpisodeStep.Dormant || progression.CurrentEpisode != NarrativeEpisode.Kitchen) return;
        Vector3 dog = playerBark.transform.position;
        if (dog.x < -0.2f || dog.x > 10.5f || dog.z > -11.2f || dog.z < -20f) return;
        StartCoroutine(BeginFirst());
    }

    private IEnumerator BeginFirst()
    {
        Step = KitchenEpisodeStep.FirstEncounter;
        firstAlien.gameObject.SetActive(true);
        onStarted.Invoke();
        yield return firstAlien.Provoke(toyPrefab, toyLanding);
    }

    private bool CanHear(KitchenAlien alien)
    {
        if (!alien || !alien.gameObject.activeInHierarchy || !beam.IsBeamActive || progression.CurrentEpisode != NarrativeEpisode.Kitchen) return false;
        Vector3 origin = playerBark.Origin.position;
        Vector3 delta = alien.ListeningPoint.position - origin;
        if (delta.sqrMagnitude > hearingRange * hearingRange || Vector3.Angle(Vector3.ProjectOnPlane(playerBark.Origin.forward, Vector3.up),
            Vector3.ProjectOnPlane(delta, Vector3.up)) > hearingAngle) return false;
        return !Physics.Linecast(origin, alien.ListeningPoint.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<KitchenAlien>() == alien;
    }

    public void OnBarkEmitted()
    {
        var alien = ActiveAlien;
        if (arrived && progression.CurrentEpisode == NarrativeEpisode.Kitchen)
        {
            ownsBarkFocus = true;
            beam.SetInteractionFocus(alien ? alien.ListeningPoint : null);
        }
        if (!CanHear(alien)) return;
        HeardBarks++;
        onHeardBark.Invoke();
    }

    private void OnInterferenceAccepted()
    {
        var alien = ActiveAlien;
        if (!CanHear(alien) || playerBark.BarkCount == consumedBark) return;
        if (Step == KitchenEpisodeStep.FirstEncounter && alien.State != KitchenAlienState.Provoking) return;
        if (Step == KitchenEpisodeStep.SecondEncounter && alien.State != KitchenAlienState.WaitingForBark) return;
        if (Step == KitchenEpisodeStep.Repairing && alien.State != KitchenAlienState.Repairing) return;
        consumedBark = playerBark.BarkCount;
        AcceptedInterventions++;
        if (Step == KitchenEpisodeStep.FirstEncounter) { Step = KitchenEpisodeStep.FirstEscape; StartCoroutine(FinishFirst()); }
        else if (Step == KitchenEpisodeStep.SecondEncounter) { Step = KitchenEpisodeStep.FirstRun; StartCoroutine(RunToRepair()); }
        else { Step = KitchenEpisodeStep.FinalEscape; StartCoroutine(FinishSecond()); }
    }

    private IEnumerator FinishFirst()
    {
        yield return firstAlien.Startle();
        yield return firstAlien.Travel(firstContactRoute);
        firstAlien.SetState(KitchenAlienState.ReleasingShutter);
        yield return new WaitForSeconds(0.25f);
        firstShutter.Release();
        yield return new WaitUntil(() => firstShutter.IsClosed);
        onFirstSealed.Invoke();
        StartCoroutine(Exit(firstAlien, firstExitRoute));
        if (!secondEncounterReady) yield break;
        secondAlien.gameObject.SetActive(true);
        secondAlien.SetState(KitchenAlienState.WaitingForBark);
        Step = KitchenEpisodeStep.SecondEncounter;
    }

    private IEnumerator RunToRepair()
    {
        yield return secondAlien.Startle();
        yield return secondAlien.Travel(repairRoute);
        secondAlien.SetState(KitchenAlienState.Repairing);
        Step = KitchenEpisodeStep.Repairing;
        if (repairParticles) repairParticles.Play();
        onRepairStarted.Invoke();
    }

    private IEnumerator FinishSecond()
    {
        if (repairParticles) repairParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        onRepairInterrupted.Invoke();
        yield return secondAlien.Startle();
        yield return secondAlien.Travel(secondContactRoute);
        secondAlien.SetState(KitchenAlienState.ReleasingShutter);
        yield return new WaitForSeconds(0.25f);
        secondShutter.Release();
        yield return new WaitUntil(() => secondShutter.IsClosed);
        if (!firstShutter.IsClosed) yield break;
        Step = KitchenEpisodeStep.Complete;
        CompletionCount++;
        StartCoroutine(Exit(secondAlien, secondExitRoute));
        progression.CompleteKitchen();
        journey.SetNarrativeHold(false);
        onCompleted.Invoke();
    }

    private IEnumerator Exit(KitchenAlien alien, Transform[] route)
    {
        yield return alien.Travel(route);
        alien.SetState(KitchenAlienState.Finished);
    }

    private void OnDisable()
    {
        if (beam) beam.InterferenceAccepted.RemoveListener(OnInterferenceAccepted);
        if (beam && ownsBarkFocus) beam.SetInteractionFocus(null);
        if (repairParticles) repairParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}


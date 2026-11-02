using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum KitchenEpisodeStep { Dormant, FirstEncounter, MovingToHide, FirstHidden, FirstRetreat, SecondEncounter, Repairing, FinalEscape, Complete }

public sealed class KitchenEpisode : MonoBehaviour
{
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private BathroomBasketDrop bathroom;
    [SerializeField] private PlayerBark playerBark;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private KitchenAlien firstAlien, secondAlien;
    [SerializeField] private AbductionSequenceAudio repairConnectionAudio;
    [SerializeField] private Rigidbody toyPrefab;
    [SerializeField] private Transform toyLanding;
    [SerializeField] private Transform[] hidingRoute, windowReturnRoute, secondExitRoute;
    [SerializeField] private Transform windowStaging, windowOutside, windowDeparture, firstExteriorRetreat;
    [Header("Salida final de B por la ventana funcional de sala")]
    [SerializeField] private Transform secondWindowStaging, secondWindowOutside, secondWindowDeparture;
    [SerializeField] private Transform secondExteriorRetreat;
    [SerializeField] private Collider secondWindowGlass;
    [SerializeField] private GameObject hidingCue;
    [SerializeField] private int arrivalWaypoint = 19;
    [SerializeField, Min(0.1f)] private float hearingRange = 4.5f;
    [SerializeField, Range(0f, 90f)] private float hearingAngle = 65f;
    [Header("Ladridos válidos para ahuyentar: cada emisión cuenta una vez")]
    [SerializeField, Min(1)] private int firstEncounterBarks = 3;
    [SerializeField, Min(1)] private int hiddenEncounterBarks = 3;
    [SerializeField, Min(1)] private int repairBarks = 3;
    [SerializeField] private int currentBarkCount;
    [SerializeField] private bool secondEncounterReady;
    [Header("Eventos locales")]
    [SerializeField] private UnityEvent onStarted = new UnityEvent();
    [SerializeField] private UnityEvent onHeardBark = new UnityEvent();
    [SerializeField] private UnityEvent onValidBark = new UnityEvent();
    [SerializeField] private UnityEvent onHiding = new UnityEvent();
    [SerializeField] private UnityEvent onDiscovered = new UnityEvent();
    [SerializeField] private UnityEvent onFirstRetreated = new UnityEvent();
    [SerializeField] private UnityEvent onRepairStarted = new UnityEvent();
    [SerializeField] private UnityEvent onRepairInterrupted = new UnityEvent();
    [SerializeField] private UnityEvent onCompleted = new UnityEvent();
    private bool arrived, ownsBarkFocus, discovered;
    private bool repairConnectionPlayed;
    private int consumedBark = -1;
    public KitchenEpisodeStep Step { get; private set; }
    public int AcceptedInterventions { get; private set; }
    public int HeardBarks { get; private set; }
    public int CompletionCount { get; private set; }
    public int CurrentBarkCount => currentBarkCount;
    public int BarksRequired => Mathf.Max(1, Step == KitchenEpisodeStep.FirstHidden ? hiddenEncounterBarks
        : Step == KitchenEpisodeStep.Repairing ? repairBarks : firstEncounterBarks);
    public bool SecondAlienGone => secondAlien && !secondAlien.gameObject.activeSelf;
    public KitchenAlien ActiveAlien => Step == KitchenEpisodeStep.FirstEncounter || Step == KitchenEpisodeStep.FirstHidden ? firstAlien
        : Step == KitchenEpisodeStep.SecondEncounter || Step == KitchenEpisodeStep.Repairing ? secondAlien : null;

    private void OnEnable() { if (beam) beam.InterferenceAccepted.AddListener(OnInterferenceAccepted); }

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
        if (Step == KitchenEpisodeStep.FirstHidden && alien.State != KitchenAlienState.Hiding) return;
        if ((Step == KitchenEpisodeStep.SecondEncounter || Step == KitchenEpisodeStep.Repairing) &&
            alien.State != KitchenAlienState.Repairing) return;
        consumedBark = playerBark.BarkCount;
        AcceptedInterventions++;
        currentBarkCount++;
        alien.ReactToBark();
        onValidBark.Invoke();
        if (Step == KitchenEpisodeStep.FirstHidden && !discovered)
        {
            discovered = true;
            if (hidingCue) hidingCue.SetActive(false);
            onDiscovered.Invoke();
        }
        if (currentBarkCount < BarksRequired) return;
        currentBarkCount = 0;
        if (Step == KitchenEpisodeStep.FirstEncounter) { Step = KitchenEpisodeStep.MovingToHide; StartCoroutine(HideFirst()); }
        else if (Step == KitchenEpisodeStep.FirstHidden) { Step = KitchenEpisodeStep.FirstRetreat; StartCoroutine(RetreatFirst()); }
        else { Step = KitchenEpisodeStep.FinalEscape; StartCoroutine(FinishSecond()); }
    }

    private IEnumerator HideFirst()
    {
        yield return firstAlien.Startle();
        yield return firstAlien.Travel(hidingRoute, true, true);
        firstAlien.SetState(KitchenAlienState.Hiding);
        Step = KitchenEpisodeStep.FirstHidden;
        if (hidingCue) hidingCue.SetActive(true);
        onHiding.Invoke();
    }

    private IEnumerator RetreatFirst()
    {
        yield return firstAlien.Startle();
        yield return firstAlien.Travel(windowReturnRoute, true, true);
        yield return firstAlien.EscapeWindow(windowStaging, windowOutside, windowDeparture);
        yield return firstAlien.RetreatExterior(firstExteriorRetreat);
        onFirstRetreated.Invoke();
        if (!secondEncounterReady) yield break;
        Step = KitchenEpisodeStep.SecondEncounter;
        secondAlien.gameObject.SetActive(true);
        secondAlien.SetState(KitchenAlienState.Repairing);
        Step = KitchenEpisodeStep.Repairing;
        currentBarkCount = 0;
        consumedBark = -1;
        ownsBarkFocus = true;
        beam.SetInteractionFocus(secondAlien.ListeningPoint);
        secondAlien.SetRemoteActive(true);
        if (!repairConnectionPlayed && repairConnectionAudio)
        {
            repairConnectionPlayed = true;
            repairConnectionAudio.PlaySequenceAt(secondAlien.ListeningPoint.position);
        }
        onRepairStarted.Invoke();
    }

    private IEnumerator FinishSecond()
    {
        secondAlien.SetRemoteActive(false);
        onRepairInterrupted.Invoke();
        yield return secondAlien.Startle();
        yield return secondAlien.Travel(secondExitRoute, true, true);
        if (secondWindowStaging && secondWindowOutside && secondWindowDeparture &&
            secondExteriorRetreat && secondWindowGlass)
        {
            yield return secondAlien.EscapeWindow(secondWindowStaging, secondWindowOutside,
                secondWindowDeparture, secondWindowGlass);
            yield return secondAlien.RetreatExterior(secondExteriorRetreat);
        }
        else
        {
            // Contingencia segura: nunca dejar al operador visible si falta una referencia de escena.
            secondAlien.SetState(KitchenAlienState.Finished);
            secondAlien.gameObject.SetActive(false);
        }
        Step = KitchenEpisodeStep.Complete;
        CompletionCount++;
        beam.SetInteractionFocus(null);
        ownsBarkFocus = false;
        progression.CompleteKitchen();
        journey.SetNarrativeHold(false);
        onCompleted.Invoke();
    }

    private void OnDisable()
    {
        if (beam) beam.InterferenceAccepted.RemoveListener(OnInterferenceAccepted);
        if (beam && ownsBarkFocus) { beam.SetInteractionFocus(null); beam.EndKitchenRepair(); }
        if (hidingCue) hidingCue.SetActive(false);
        if (secondAlien) secondAlien.SetRemoteActive(false);
    }
}

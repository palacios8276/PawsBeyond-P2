using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public sealed class ReturnCliffhanger : MonoBehaviour
{
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private KitchenEpisode kitchen;
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private DoorTriggerDetector bedroomTrigger;
    [SerializeField] private HingeJoint bedroomDoor;
    [SerializeField] private Transform player;
    [SerializeField] private PlayerGameplayLock gameplayLock;
    [SerializeField] private int bedroomArrivalWaypoint = 30;
    [SerializeField] private Light glow;
    [SerializeField] private GameObject partialFigure;
    [SerializeField] private ClosingCameraApproach closingCameraApproach;
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField] private TMP_Text closingText;
    [SerializeField, Min(0.1f)] private float glowSeconds = 1f;
    [SerializeField, Min(0f)] private float revealHoldSeconds = 0.8f;
    [SerializeField, Min(0f)] private float closingCameraCueDelay = 1.1f;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;
    [SerializeField, Min(0f)] private float glowIntensity = 6f;
    [SerializeField] private UnityEvent onGlowStarted = new UnityEvent();
    [SerializeField] private UnityEvent onFigureRevealed = new UnityEvent();
    [SerializeField] private UnityEvent onContinueShown = new UnityEvent();
    [SerializeField] private UnityEvent onBarkRequested = new UnityEvent();
    [SerializeField] private UnityEvent onBarkOpportunityEnded = new UnityEvent();

    private BoxCollider zone;
    private bool playerInside, ownersArrived, closingRequested, barkPromptShown;

    public int TriggerCount { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsEligible => progression.CurrentEpisode == NarrativeEpisode.ReturnAndClosing
        && kitchen.Step == KitchenEpisodeStep.Complete && ownersArrived;
    public bool IsAwaitingBark => !closingRequested && TriggerCount == 0 && playerInside && IsEligible;

    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        ownersArrived = journey && journey.HasReachedWaypoint(bedroomArrivalWaypoint);
        playerInside = PlayerIsInside();
        if (glow) glow.intensity = 0;
        if (partialFigure) partialFigure.SetActive(false);
        if (blackScreen) blackScreen.alpha = 0;
        if (closingText) closingText.alpha = 0;
    }

    private void OnEnable()
    {
        if (journey) journey.WaypointReached += OnWaypointReached;
    }

    private void OnDisable()
    {
        if (journey) journey.WaypointReached -= OnWaypointReached;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInside = true;
        TryRequestClosing();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInside = false;
        if (barkPromptShown && !closingRequested)
        {
            barkPromptShown = false;
            onBarkOpportunityEnded.Invoke();
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (!other || !player) return false;
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        return controller && controller.transform == player;
    }

    private bool PlayerIsInside()
    {
        return zone && player && zone.bounds.Contains(player.position + Vector3.up * 0.3f);
    }

    private void OnWaypointReached(int index)
    {
        if (index < bedroomArrivalWaypoint) return;
        ownersArrived = true;
        TryRequestClosing();
    }

    private void Update()
    {
        playerInside = PlayerIsInside();
        if (!ownersArrived && journey && journey.HasReachedWaypoint(bedroomArrivalWaypoint))
            ownersArrived = true;
        TryRequestClosing();
    }

    private void TryRequestClosing()
    {
        if (!IsAwaitingBark || barkPromptShown) return;
        barkPromptShown = true;
        onBarkRequested.Invoke();
    }

    public void TryBeginClosingFromBark()
    {
        if (!IsAwaitingBark) return;
        closingRequested = true;
        progression.RequestClosing();
    }

    public void BeginClosing()
    {
        if (TriggerCount != 0 || !IsEligible || !PlayerIsInside()) return;
        closingRequested = true;
        TriggerCount++;
        if (gameplayLock) gameplayLock.LockForClosing();
        StartCoroutine(Present());
    }

    private IEnumerator Present()
    {
        journey.SetNarrativeHold(true);
        bedroomTrigger.RequestNarrativeOpening(true);
        while (float.IsNaN(bedroomDoor.angle) || Mathf.Abs(bedroomDoor.angle) < 75f)
            yield return null;

        onGlowStarted.Invoke();
        if (partialFigure) partialFigure.SetActive(true);

        bool cameraStarted = false;
        float elapsed = 0f;
        while (elapsed < glowSeconds)
        {
            if (!cameraStarted && elapsed >= closingCameraCueDelay)
            {
                StartClosingCameraCue();
                cameraStarted = true;
            }

            if (glow)
                glow.intensity = glowIntensity * Mathf.SmoothStep(0f, 1f, elapsed / glowSeconds);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (glow) glow.intensity = glowIntensity;
        if (!cameraStarted)
        {
            float remainingDelay = Mathf.Max(0f, closingCameraCueDelay - elapsed);
            if (remainingDelay > 0f) yield return new WaitForSeconds(remainingDelay);
            StartClosingCameraCue();
        }

        while (closingCameraApproach && closingCameraApproach.IsMoving)
            yield return null;

        yield return new WaitForSeconds(revealHoldSeconds);

        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            if (blackScreen) blackScreen.alpha = Mathf.SmoothStep(0f, 1f, t / fadeSeconds);
            yield return null;
        }

        if (blackScreen) blackScreen.alpha = 1f;
        if (glow) glow.intensity = 0f;
        if (partialFigure) partialFigure.SetActive(false);
        bedroomTrigger.RequestNarrativeOpening(false);
        journey.SetNarrativeHold(false);

        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            if (closingText) closingText.alpha = Mathf.SmoothStep(0f, 1f, t / fadeSeconds);
            yield return null;
        }

        if (closingText) closingText.alpha = 1f;
        IsComplete = true;
        onContinueShown.Invoke();
    }

    private void StartClosingCameraCue()
    {
        onFigureRevealed.Invoke();
        if (closingCameraApproach) closingCameraApproach.BeginApproach();
    }
}
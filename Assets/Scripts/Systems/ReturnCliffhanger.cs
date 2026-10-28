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
    [SerializeField] private int bedroomArrivalWaypoint = 30;
    [SerializeField] private Light glow;
    [SerializeField] private GameObject partialFigure;
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField] private TMP_Text closingText;
    [SerializeField, Min(0.1f)] private float glowSeconds = 1f;
    [SerializeField, Min(0f)] private float revealHoldSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;
    [SerializeField, Min(0f)] private float glowIntensity = 6f;
    [SerializeField] private UnityEvent onGlowStarted = new UnityEvent();
    [SerializeField] private UnityEvent onFigureRevealed = new UnityEvent();
    [SerializeField] private UnityEvent onContinueShown = new UnityEvent();
    private BoxCollider zone;
    public int TriggerCount { get; private set; }
    public bool IsComplete { get; private set; }
    public bool IsEligible => progression.CurrentEpisode == NarrativeEpisode.ReturnAndClosing
        && kitchen.Step == KitchenEpisodeStep.Complete && journey.CurrentWaypoint >= bedroomArrivalWaypoint
        && journey.transform.position.x < -13.5f;
    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        if (glow) glow.intensity = 0;
        if (partialFigure) partialFigure.SetActive(false);
        if (blackScreen) blackScreen.alpha = 0;
        if (closingText) closingText.alpha = 0;
    }
    private void Update()
    {
        // Revalidar la posición permite llegar antes o después de los dueños.
        if (TriggerCount == 0 && IsEligible && zone.bounds.Contains(player.position + Vector3.up * 0.3f))
            progression.RequestClosing();
    }
    public void BeginClosing()
    {
        if (TriggerCount != 0 || !IsEligible || !zone.bounds.Contains(player.position + Vector3.up * 0.3f)) return;
        TriggerCount++;
        StartCoroutine(Present());
    }
    private IEnumerator Present()
    {
        journey.SetNarrativeHold(true);
        bedroomTrigger.RequestNarrativeOpening(true);
        while (float.IsNaN(bedroomDoor.angle) || Mathf.Abs(bedroomDoor.angle) < 75f) yield return null;
        onGlowStarted.Invoke();
        partialFigure.SetActive(true);
        onFigureRevealed.Invoke();
        for (float t = 0; t < glowSeconds; t += Time.deltaTime)
        { glow.intensity = glowIntensity * Mathf.SmoothStep(0, 1, t / glowSeconds); yield return null; }
        glow.intensity = glowIntensity;
        yield return new WaitForSeconds(revealHoldSeconds);
        for (float t = 0; t < fadeSeconds; t += Time.deltaTime)
        { blackScreen.alpha = Mathf.SmoothStep(0, 1, t / fadeSeconds); yield return null; }
        blackScreen.alpha = 1;
        glow.intensity = 0;
        partialFigure.SetActive(false);
        bedroomTrigger.RequestNarrativeOpening(false);
        journey.SetNarrativeHold(false);
        for (float t = 0; t < fadeSeconds; t += Time.deltaTime)
        { closingText.alpha = Mathf.SmoothStep(0, 1, t / fadeSeconds); yield return null; }
        closingText.alpha = 1;
        IsComplete = true;
        onContinueShown.Invoke();
    }
}

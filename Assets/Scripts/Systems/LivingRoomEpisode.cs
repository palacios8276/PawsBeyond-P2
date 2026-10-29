using UnityEngine;
using UnityEngine.Events;

public sealed class LivingRoomEpisode : MonoBehaviour
{
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private GameObject alienSilhouette;
    [SerializeField] private Light functionalWindowLight, blockedWindowLight;
    [SerializeField] private int functionalWindowWaypoint = 4;
    [SerializeField] private int blockedWindowWaypoint = 5;
    [SerializeField, Min(0.1f)] private float blockedAttemptSeconds = 1.5f;
    [SerializeField] private UnityEvent onWindowAttempt = new UnityEvent();
    [SerializeField] private UnityEvent onRedirected = new UnityEvent();
    [SerializeField] private UnityEvent onWindowFocusShifted = new UnityEvent();
    private bool attempted, waitingForBarks, redirected, atBlockedWindow, completed;
    private float blockedUntil;
    public bool IsWaitingForBarks => waitingForBarks && !completed;

    public void OnWaypointReached(int index)
    {
        if (completed || progression.CurrentEpisode != NarrativeEpisode.LivingRoom) return;
        if (index == functionalWindowWaypoint && !attempted)
        {
            attempted = true; waitingForBarks = true;
            journey.SetNarrativeHold(true);
            if (alienSilhouette) alienSilhouette.SetActive(true);
            if (functionalWindowLight) functionalWindowLight.enabled = true;
            onWindowAttempt.Invoke();
            // Acepta la interferencia lograda durante la aproximación, sin exigir otro ciclo.
            if (beam.CurrentState == AbductionBeamState.Uncontrolled) OnBeamUncontrolled();
        }
        else if (index == blockedWindowWaypoint && redirected && !atBlockedWindow)
        {
            atBlockedWindow = true;
            journey.SetNarrativeHold(true);
            blockedUntil = Time.time + blockedAttemptSeconds;
            if (functionalWindowLight) functionalWindowLight.enabled = false;
            if (blockedWindowLight) blockedWindowLight.enabled = true;
            if (alienSilhouette) alienSilhouette.SetActive(false);
        }
    }

    public void OnBeamUncontrolled()
    {
        if (!waitingForBarks || redirected || completed || !beam.IsBeamActive ||
            progression.CurrentEpisode != NarrativeEpisode.LivingRoom) return;
        redirected = true; waitingForBarks = false;
        if (functionalWindowLight) functionalWindowLight.enabled = false;
        if (blockedWindowLight) blockedWindowLight.enabled = true;
        if (alienSilhouette) alienSilhouette.SetActive(false);
        onWindowFocusShifted.Invoke();
        journey.SetNarrativeHold(false);
    }

    private void Update()
    {
        if (!atBlockedWindow || completed || Time.time < blockedUntil) return;
        completed = true;
        if (blockedWindowLight) blockedWindowLight.enabled = false;
        progression.CompleteLivingRoom();
        journey.SetNarrativeHold(false);
        onRedirected.Invoke();
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public sealed class ContextualCameraDirector : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera abductionCamera;
    [SerializeField] private CinemachineCamera alienReactionCamera;
    [SerializeField] private int inactivePriority = -10;
    [SerializeField] private int activePriority = 30;

    [Header("Condición narrativa")]
    [SerializeField] private BedroomTutorial bedroomTutorial;
    [SerializeField] private PlayerGameplayLock permanentGameplayLock;

    [Header("Ritmo")]
    [SerializeField, Min(0f)] private float abductionMinimumSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float abductionSafetyTimeout = 7f;
    [SerializeField, Min(0.1f)] private float alienReactionSeconds = 0.85f;
    [SerializeField, Min(0f)] private float returnBlendSeconds = 0.25f;

    [Header("Controles temporales")]
    [SerializeField] private MonoBehaviour[] abductionControls;
    [SerializeField] private MonoBehaviour[] alienReactionControls;

    private readonly Dictionary<MonoBehaviour, bool> savedControlStates = new Dictionary<MonoBehaviour, bool>();
    private Coroutine activeSequence;
    private CinemachineCamera activeCamera;
    private bool abductionPlayed;
    private bool alienReactionPlayed;

    public bool IsPlaying => activeSequence != null;
    public int AbductionPlayCount { get; private set; }
    public int AlienReactionPlayCount { get; private set; }
    public string ActiveCameraName => activeCamera ? activeCamera.name : string.Empty;

    private void Awake()
    {
        SetPriority(abductionCamera, inactivePriority);
        SetPriority(alienReactionCamera, inactivePriority);
    }

    public void PlayAbduction()
    {
        if (abductionPlayed || IsPlaying || !abductionCamera) return;
        abductionPlayed = true;
        AbductionPlayCount++;
        activeSequence = StartCoroutine(AbductionSequence());
    }

    public void PlayAlienAReaction()
    {
        if (alienReactionPlayed || IsPlaying || !alienReactionCamera) return;
        alienReactionPlayed = true;
        AlienReactionPlayCount++;
        activeSequence = StartCoroutine(TimedSequence(alienReactionCamera,
            alienReactionControls, alienReactionSeconds));
    }

    private IEnumerator AbductionSequence()
    {
        BeginSequence(abductionCamera, abductionControls);
        float startedAt = Time.unscaledTime;

        while (Time.unscaledTime - startedAt < abductionMinimumSeconds)
            yield return null;

        while (bedroomTutorial && bedroomTutorial.Step == BedroomTutorialStep.Abduction
            && Time.unscaledTime - startedAt < abductionSafetyTimeout)
            yield return null;

        yield return EndSequence();
    }

    private IEnumerator TimedSequence(CinemachineCamera camera, MonoBehaviour[] controls, float duration)
    {
        BeginSequence(camera, controls);
        float endAt = Time.unscaledTime + duration;
        while (Time.unscaledTime < endAt) yield return null;
        yield return EndSequence();
    }

    private void BeginSequence(CinemachineCamera camera, MonoBehaviour[] controls)
    {
        activeCamera = camera;
        SaveAndDisable(controls);
        SetPriority(camera, activePriority);
    }

    private IEnumerator EndSequence()
    {
        SetPriority(activeCamera, inactivePriority);
        float endAt = Time.unscaledTime + returnBlendSeconds;
        while (Time.unscaledTime < endAt) yield return null;

        RestoreControls();
        activeCamera = null;
        activeSequence = null;
    }

    private void SaveAndDisable(MonoBehaviour[] controls)
    {
        savedControlStates.Clear();
        if (controls == null) return;

        foreach (MonoBehaviour control in controls)
        {
            if (!control || savedControlStates.ContainsKey(control)) continue;
            savedControlStates.Add(control, control.enabled);
            control.enabled = false;
        }
    }

    private void RestoreControls()
    {
        bool permanentlyLocked = permanentGameplayLock && permanentGameplayLock.IsLocked;
        foreach (KeyValuePair<MonoBehaviour, bool> state in savedControlStates)
            if (state.Key) state.Key.enabled = !permanentlyLocked && state.Value;
        savedControlStates.Clear();
    }

    private static void SetPriority(CinemachineCamera camera, int priority)
    {
        if (camera) camera.Priority = priority;
    }

    private void OnDisable()
    {
        if (activeSequence != null) StopCoroutine(activeSequence);
        SetPriority(abductionCamera, inactivePriority);
        SetPriority(alienReactionCamera, inactivePriority);
        RestoreControls();
        activeCamera = null;
        activeSequence = null;
    }
}

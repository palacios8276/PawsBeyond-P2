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
    [SerializeField] private CinemachineCamera closingRetreatCamera;
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private int inactivePriority = -10;
    [SerializeField] private int activePriority = 30;

    [Header("Condición narrativa")]
    [SerializeField] private BedroomTutorial bedroomTutorial;
    [SerializeField] private KitchenAlien alienA;
    [SerializeField] private PlayerGameplayLock permanentGameplayLock;

    [Header("Ritmo")]
    [SerializeField, Min(0f)] private float abductionMinimumSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float abductionSafetyTimeout = 7f;
    [SerializeField, Min(0.1f)] private float alienReactionSeconds = 0.85f;
    [SerializeField, Min(0.1f)] private float alienReactionSafetyTimeout = 5f;
    [SerializeField, Min(0f)] private float returnBlendSeconds = 0.25f;
    [SerializeField, Min(0.1f)] private float returnBlendSafetyTimeout = 3f;

    [Header("Controles temporales")]
    [Tooltip("Lectores de órbita comunes a todas las cámaras contextuales.")]
    [SerializeField] private MonoBehaviour[] orbitControls;
    [SerializeField] private MonoBehaviour[] abductionControls;
    [SerializeField] private MonoBehaviour[] alienReactionControls;

    private readonly Dictionary<MonoBehaviour, bool> savedControlStates = new Dictionary<MonoBehaviour, bool>();
    private Coroutine activeSequence;
    private CinemachineCamera activeCamera;
    private bool abductionPlayed;
    private bool alienReactionPlayed;
    private bool closingRetreatPlaying;

    public bool IsPlaying => activeSequence != null || closingRetreatPlaying;
    public bool IsClosingRetreatPlaying => closingRetreatPlaying;
    public int AbductionPlayCount { get; private set; }
    public int AlienReactionPlayCount { get; private set; }
    public string ActiveCameraName => activeCamera ? activeCamera.name : string.Empty;

    private void Awake()
    {
        if (!brain && Camera.main) brain = Camera.main.GetComponent<CinemachineBrain>();
        SetPriority(abductionCamera, inactivePriority);
        SetPriority(alienReactionCamera, inactivePriority);
        SetPriority(closingRetreatCamera, inactivePriority);
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
        activeSequence = StartCoroutine(AlienReactionSequence());
    }

    public void PlayClosingRetreat()
    {
        if (closingRetreatPlaying || !closingRetreatCamera) return;

        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
            SetPriority(activeCamera, inactivePriority);
            RestoreControls();
        }

        closingRetreatPlaying = true;
        BeginSequence(closingRetreatCamera, null);
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

    private IEnumerator AlienReactionSequence()
    {
        BeginSequence(alienReactionCamera, alienReactionControls);
        float startedAt = Time.unscaledTime;

        while (Time.unscaledTime - startedAt < alienReactionSeconds)
            yield return null;

        while (alienA && IsAlienReactionActive(alienA.State) &&
            Time.unscaledTime - startedAt < alienReactionSafetyTimeout)
            yield return null;

        yield return EndSequence();
    }

    private static bool IsAlienReactionActive(KitchenAlienState state) =>
        state == KitchenAlienState.Startled || state == KitchenAlienState.Running;

    private void BeginSequence(CinemachineCamera camera, MonoBehaviour[] controls)
    {
        activeCamera = camera;
        savedControlStates.Clear();
        SaveAndDisable(orbitControls, false);
        SaveAndDisable(controls, false);
        SetPriority(camera, activePriority);
    }

    private IEnumerator EndSequence()
    {
        SetPriority(activeCamera, inactivePriority);
        float minimumEndAt = Time.unscaledTime + returnBlendSeconds;
        float timeoutAt = Time.unscaledTime + returnBlendSafetyTimeout;
        while (Time.unscaledTime < timeoutAt)
        {
            bool minimumElapsed = Time.unscaledTime >= minimumEndAt;
            bool gameplayHasControl = !brain ||
                (!brain.IsBlending && ReferenceEquals(brain.ActiveVirtualCamera, gameplayCamera));
            if (minimumElapsed && gameplayHasControl) break;
            yield return null;
        }

        RestoreControls();
        activeCamera = null;
        activeSequence = null;
    }

    private void SaveAndDisable(MonoBehaviour[] controls, bool clearSaved = true)
    {
        if (clearSaved) savedControlStates.Clear();
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
        SetPriority(closingRetreatCamera, inactivePriority);
        RestoreControls();
        closingRetreatPlaying = false;
        activeCamera = null;
        activeSequence = null;
    }
}

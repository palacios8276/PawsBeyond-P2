using UnityEngine;

// Traduce el estado jugable existente a los clips del modelo visual, sin controlar gameplay.
public sealed class DogModelAnimatorBridge : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerBark bark;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField, Min(0f)] private float movementThreshold = 0.08f;
    [SerializeField, Min(0f)] private float runThreshold = 3.2f;
    [SerializeField, Min(0.05f)] private float barkDuration = 0.75f;
    [SerializeField, Min(0f)] private float minimumBarkPoseSeconds = 0.16f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.12f;
    [Header("Sincronización de locomoción")]
    [SerializeField, Min(0.1f)] private float walkReferenceSpeed = 2f;
    [SerializeField, Min(0.1f)] private float runReferenceSpeed = 5f;
    [SerializeField, Min(0.1f)] private float walkPlaybackMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float runPlaybackMultiplier = 1.15f;
    [SerializeField] private Vector2 playbackSpeedRange = new Vector2(0.55f, 1.75f);
    [SerializeField, Min(0f)] private float playbackSmoothing = 8f;
    [SerializeField, Min(0f)] private float locomotionSpeedSmoothing = 12f;
    [SerializeField, Min(0.5f)] private float teleportDistance = 2f;

    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int WalkState = Animator.StringToHash("Walk");
    private static readonly int RunState = Animator.StringToHash("Run");
    private static readonly int BarkState = Animator.StringToHash("Bark");
    private static readonly int CarryState = Animator.StringToHash("Carry");

    private int observedBarkCount;
    private int currentState;
    private float barkEndsAt;
    private float barkCanBeInterruptedAt;
    private Vector3 previousPosition;
    private bool hasPreviousPosition;
    public float CurrentPlanarSpeed { get; private set; }
    public float CurrentPlaybackSpeed => animator ? animator.speed : 0f;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!controller) controller = GetComponentInParent<CharacterController>();
        if (!bark) bark = GetComponentInParent<PlayerBark>();
        if (!interaction) interaction = GetComponentInParent<PlayerInteraction>();

        if (animator)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            currentState = IdleState;
            animator.Play(IdleState, 0, 0f);
        }

        observedBarkCount = bark ? bark.BarkCount : 0;
        ResetPositionSample();
    }

    private void Update()
    {
        if (!animator) return;

        float planarSpeed = MeasurePlanarSpeed();

        if (bark && bark.BarkCount != observedBarkCount)
        {
            observedBarkCount = bark.BarkCount;
            barkEndsAt = Time.time + barkDuration;
            barkCanBeInterruptedAt = Time.time + Mathf.Min(minimumBarkPoseSeconds, barkDuration);
            currentState = BarkState;
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(BarkState, Mathf.Min(0.05f, transitionDuration), 0, 0f);
            return;
        }

        if (Time.time < barkEndsAt &&
            (Time.time < barkCanBeInterruptedAt || planarSpeed <= movementThreshold)) return;

        int desiredState = interaction && interaction.IsDragging
            ? CarryState
            : planarSpeed >= runThreshold
                ? RunState
                : planarSpeed > movementThreshold ? WalkState : IdleState;

        UpdatePlaybackSpeed(planarSpeed, desiredState);
        if (desiredState != currentState)
        {
            currentState = desiredState;
            animator.CrossFadeInFixedTime(desiredState, transitionDuration);
        }
    }

    private float MeasurePlanarSpeed()
    {
        Transform observed = controller ? controller.transform : transform;
        Vector3 position = observed.position;
        if (!hasPreviousPosition || Time.deltaTime <= 0f)
        {
            previousPosition = position;
            hasPreviousPosition = true;
            CurrentPlanarSpeed = 0f;
            return 0f;
        }

        Vector3 delta = Vector3.ProjectOnPlane(position - previousPosition, Vector3.up);
        previousPosition = position;
        float rawSpeed = delta.magnitude > teleportDistance ? 0f : delta.magnitude / Time.deltaTime;
        CurrentPlanarSpeed = locomotionSpeedSmoothing <= 0f
            ? rawSpeed
            : Mathf.MoveTowards(CurrentPlanarSpeed, rawSpeed, locomotionSpeedSmoothing * Time.deltaTime);
        return CurrentPlanarSpeed;
    }

    private void ResetPositionSample()
    {
        Transform observed = controller ? controller.transform : transform;
        previousPosition = observed.position;
        hasPreviousPosition = true;
        CurrentPlanarSpeed = 0f;
    }

    private void UpdatePlaybackSpeed(float planarSpeed, int desiredState)
    {
        float target = 1f;
        if (desiredState == WalkState)
            target = planarSpeed / walkReferenceSpeed * walkPlaybackMultiplier;
        else if (desiredState == RunState)
            target = planarSpeed / runReferenceSpeed * runPlaybackMultiplier;
        else if (desiredState == IdleState)
            target = 1f;

        target = Mathf.Clamp(target, playbackSpeedRange.x, playbackSpeedRange.y);
        animator.speed = playbackSmoothing <= 0f
            ? target
            : Mathf.MoveTowards(animator.speed, target, playbackSmoothing * Time.deltaTime);
    }

    private void OnDisable()
    {
        if (animator) animator.speed = 1f;
        CurrentPlanarSpeed = 0f;
        hasPreviousPosition = false;
    }
}

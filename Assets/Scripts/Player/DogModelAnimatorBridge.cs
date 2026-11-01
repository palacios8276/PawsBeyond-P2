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
    [SerializeField, Min(0f)] private float transitionDuration = 0.12f;

    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int WalkState = Animator.StringToHash("Walk");
    private static readonly int RunState = Animator.StringToHash("Run");
    private static readonly int BarkState = Animator.StringToHash("Bark");
    private static readonly int CarryState = Animator.StringToHash("Carry");

    private int observedBarkCount;
    private int currentState;
    private float barkEndsAt;

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
    }

    private void Update()
    {
        if (!animator) return;

        if (bark && bark.BarkCount != observedBarkCount)
        {
            observedBarkCount = bark.BarkCount;
            barkEndsAt = Time.time + barkDuration;
            currentState = BarkState;
            animator.CrossFadeInFixedTime(BarkState, Mathf.Min(0.05f, transitionDuration), 0, 0f);
            return;
        }

        if (Time.time < barkEndsAt) return;

        float planarSpeed = controller
            ? Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude
            : 0f;
        int desiredState = interaction && interaction.IsDragging
            ? CarryState
            : planarSpeed >= runThreshold
                ? RunState
                : planarSpeed > movementThreshold ? WalkState : IdleState;

        if (desiredState == currentState) return;
        currentState = desiredState;
        animator.CrossFadeInFixedTime(desiredState, transitionDuration);
    }
}

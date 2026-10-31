using UnityEngine;

[RequireComponent(typeof(KitchenAlien), typeof(Animator))]
public sealed class AlienAnimatorBridge : MonoBehaviour
{
    public enum AlienRole { Provoker, Repairer }

    [SerializeField] private AlienRole role;
    [SerializeField] private KitchenAlien alien;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float crossFadeSeconds = 0.12f;

    private int currentStateHash;

    private void Awake()
    {
        if (!alien) alien = GetComponent<KitchenAlien>();
        if (!animator) animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        currentStateHash = 0;
    }

    private void Update()
    {
        int targetStateHash = Animator.StringToHash(GetVisualState(alien.State));
        if (targetStateHash == currentStateHash || !animator.HasState(0, targetStateHash)) return;

        animator.CrossFadeInFixedTime(targetStateHash, crossFadeSeconds, 0);
        currentStateHash = targetStateHash;
    }

    private string GetVisualState(KitchenAlienState state)
    {
        if (state == KitchenAlienState.Startled) return "React";
        if (state == KitchenAlienState.Running || state == KitchenAlienState.Crawling) return "Run";

        if (role == AlienRole.Repairer)
        {
            if (state == KitchenAlienState.Repairing || state == KitchenAlienState.ReleasingShutter
                || state == KitchenAlienState.ThrowingOtherHand)
                return "Repair";
        }
        else if (state == KitchenAlienState.Appearing || state == KitchenAlienState.Throwing
                 || state == KitchenAlienState.ThrowingOtherHand || state == KitchenAlienState.Provoking
                 || state == KitchenAlienState.Investigating)
            return "Investigate";

        return "Idle";
    }
}

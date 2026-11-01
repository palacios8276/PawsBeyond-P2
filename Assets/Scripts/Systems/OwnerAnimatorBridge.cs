using UnityEngine;

// Traduce el estado narrativo existente a clips locales del modelo visual.
// No mueve al owner ni reemplaza OwnersJourney/OwnerFloat.
public sealed class OwnerAnimatorBridge : MonoBehaviour
{
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private Animator animator;
    [SerializeField] private string sleepingState = "Sleeping_Aligned";
    [SerializeField] private string floatingState = "Floating";
    [SerializeField, Min(0f)] private float crossFadeSeconds = 0.15f;

    private int currentState;

    private void Awake()
    {
        if (!journey) journey = GetComponentInParent<OwnersJourney>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (animator) animator.applyRootMotion = false;
    }

    private void OnEnable() => currentState = 0;

    private void Update()
    {
        if (!animator || !journey) return;
        int target = Animator.StringToHash(journey.HasStarted ? floatingState : sleepingState);
        if (target == currentState || !animator.HasState(0, target)) return;
        animator.CrossFadeInFixedTime(target, crossFadeSeconds, 0);
        currentState = target;
    }
}

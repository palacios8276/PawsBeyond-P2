using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public sealed class PlayerBark : MonoBehaviour
{
    [SerializeField] private DogBarkAudio barkAudio;
    [SerializeField, Min(0.05f)] private float minimumInterval = 0.25f;
    [SerializeField] private Transform barkOrigin;
    [SerializeField, Min(0.1f)] private float combinationWindow = 1.1f;
    [SerializeField, Range(0f, 1f)] private float normalInterference = 0.2f;
    [SerializeField, Range(0f, 1f)] private float reinforcedInterference = 0.4f;
    [SerializeField] private UnityEvent onBarked = new UnityEvent();
    private PlayerInputActions inputActions;
    private float nextBarkTime;
    private float combinationExpiresAt;
    private bool combinationPending;
    public bool IsLastBarkReinforced { get; private set; }
    public float LastInterference { get; private set; }
    public Transform Origin => barkOrigin ? barkOrigin : transform;
    public int BarkCount { get; private set; }

    private void Awake() => inputActions = new PlayerInputActions();

    private void OnEnable()
    {
        inputActions.Player.Bark.performed += OnBark;
        inputActions.Player.Bark.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Bark.performed -= OnBark;
        inputActions.Player.Bark.Disable();
        combinationPending = false;
    }

    private void OnDestroy() => inputActions?.Dispose();
    private void OnBark(InputAction.CallbackContext context) => TryBark();

    // Un único evento por pulsación aceptada; mantener la tecla no vuelve a llamarlo.
    public bool TryBark()
    {
        if (!isActiveAndEnabled || Time.time < nextBarkTime || !barkAudio) return false;
        bool reinforced = combinationPending && Time.time <= combinationExpiresAt;
        if (!barkAudio.TryPlayBark(reinforced)) return false;
        IsLastBarkReinforced = reinforced;
        LastInterference = reinforced ? reinforcedInterference : normalInterference;
        combinationPending = !reinforced;
        combinationExpiresAt = Time.time + combinationWindow;
        nextBarkTime = Time.time + (reinforced
            ? Mathf.Max(minimumInterval, barkAudio.LastPlaybackDuration) : minimumInterval);
        BarkCount++;
        onBarked.Invoke();
        return true;
    }
}

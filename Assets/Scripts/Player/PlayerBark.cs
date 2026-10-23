using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public sealed class PlayerBark : MonoBehaviour
{
    [SerializeField] private DogBarkAudio barkAudio;
    [SerializeField, Min(0.05f)] private float minimumInterval = 0.45f;
    [SerializeField] private UnityEvent onBarked = new UnityEvent();
    private PlayerInputActions inputActions;
    private float nextBarkTime;
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
    }

    private void OnDestroy() => inputActions?.Dispose();
    private void OnBark(InputAction.CallbackContext context) => TryBark();

    // Un único evento por pulsación aceptada; mantener la tecla no vuelve a llamarlo.
    public bool TryBark()
    {
        if (!isActiveAndEnabled || Time.time < nextBarkTime || !barkAudio || !barkAudio.TryPlayBark()) return false;
        nextBarkTime = Time.time + minimumInterval;
        BarkCount++;
        onBarked.Invoke();
        return true;
    }
}

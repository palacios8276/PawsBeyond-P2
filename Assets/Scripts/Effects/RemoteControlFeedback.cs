using UnityEngine;
using UnityEngine.Events;

public sealed class RemoteControlFeedback : MonoBehaviour
{
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private KitchenAlien owner;
    [SerializeField] private Transform target, button;
    [SerializeField] private Light emitter;
    [SerializeField] private Renderer indicator;
    [SerializeField, Min(0.1f)] private float pressesPerSecond = 2f;
    [SerializeField] private UnityEvent onButtonPressed = new UnityEvent();
    [SerializeField] private UnityEvent onPowerOff = new UnityEvent();
    private bool operating, previousPress;
    private Vector3 buttonPosition;
    private MaterialPropertyBlock block;
    public bool IsOperating => operating && beam && beam.IsKitchenRepairing && owner.State == KitchenAlienState.Repairing;
    public int ButtonPressCount { get; private set; }
    private void Awake() { if (button) buttonPosition = button.localPosition; block = new MaterialPropertyBlock(); }
    public void SetOperating(bool active)
    {
        if (operating && !active) onPowerOff.Invoke();
        operating = active;
        if (!active && emitter) emitter.intensity = 0f;
    }
    private void LateUpdate()
    {
        bool active = IsOperating;
        float pulse = active ? 0.5f + 0.5f * Mathf.Sin(Time.time * pressesPerSecond * Mathf.PI * 2f) : 0f;
        if (active && target) transform.rotation = Quaternion.LookRotation(target.position - transform.position);
        bool press = active && pulse > 0.8f;
        if (press && !previousPress) { ButtonPressCount++; onButtonPressed.Invoke(); }
        previousPress = press;
        if (button) button.localPosition = buttonPosition + Vector3.down * (press ? 0.012f : 0f);
        if (emitter) emitter.intensity = pulse * 0.35f;
        if (indicator) { indicator.GetPropertyBlock(block); block.SetColor("_BaseColor", Color.Lerp(new Color(0.08f, 0.12f, 0.15f), Color.cyan, pulse)); indicator.SetPropertyBlock(block); }
    }
    private void OnDisable() { operating = false; if (emitter) emitter.intensity = 0f; }
}

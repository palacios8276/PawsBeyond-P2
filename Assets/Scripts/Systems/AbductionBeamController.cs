using System;
using UnityEngine;
using UnityEngine.Events;

public enum AbductionBeamState { Stable, Affected, IntenseInterference, Uncontrolled }
[Serializable] public sealed class BeamStateChangedEvent : UnityEvent<AbductionBeamState> { }

public sealed class AbductionBeamController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerBark barkSource;
    [SerializeField] private Renderer beamRenderer;
    [SerializeField] private Light beamLight;
    [SerializeField] private AudioSource beamAudio;
    [Header("Detección")]
    [SerializeField, Min(0.1f)] private float barkRange = 6f;
    [SerializeField, Range(0f, 180f)] private float maximumFacingAngle = 70f;
    [Header("Interferencia")]
    [SerializeField, Range(0f, 1f)] private float interference;
    [SerializeField, Range(0.01f, 1f)] private float affectedThreshold = 0.2f;
    [SerializeField, Range(0.01f, 1f)] private float intenseThreshold = 0.55f;
    [SerializeField, Range(0.01f, 1f)] private float uncontrolledThreshold = 0.85f;
    [SerializeField, Min(0f)] private float recoveryDelay = 1.25f;
    [SerializeField, Min(0f)] private float recoveryPerSecond = 0.12f;
    [SerializeField] private AbductionBeamState currentState;
    [Header("Respuesta visual")]
    [SerializeField] private Color stableColor = new Color(0.2f, 0.85f, 1f, 0.2f);
    [SerializeField] private Color affectedColor = new Color(0.25f, 1f, 0.75f, 0.2f);
    [SerializeField] private Color intenseColor = new Color(1f, 0.55f, 0.12f, 0.25f);
    [SerializeField] private Color uncontrolledColor = new Color(0.8f, 0.25f, 1f, 0.3f);
    [SerializeField] private AnimationCurve lightIntensity = AnimationCurve.Linear(0f, 2.5f, 1f, 0.6f);
    [SerializeField, Min(0.1f)] private float feedbackResponse = 5f;
    [SerializeField, Range(0f, 0.5f)] private float flickerAmount = 0.25f;
    [SerializeField, Range(0f, 0.1f)] private float pulseAmount = 0.025f;
    [Header("Respuesta sonora")]
    [SerializeField, Range(0f, 1f)] private float stableVolume = 0.65f;
    [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.8f;
    [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1.35f);
    [Header("Eventos, sin redirección narrativa")]
    [SerializeField] private BeamStateChangedEvent onStateChanged = new BeamStateChangedEvent();
    [SerializeField] private UnityEvent onUncontrolled = new UnityEvent();
    [SerializeField] private bool beamActive = true;
    [SerializeField] private bool externalPresentation;
    [SerializeField] private UnityEvent onInterferenceAccepted = new UnityEvent();
    public UnityEvent InterferenceAccepted => onInterferenceAccepted;
    public bool IsBeamActive => isActiveAndEnabled && beamActive;
    public void SetBeamActive(bool active)
    {
        beamActive = active;
        if (beamRenderer) beamRenderer.enabled = active;
        if (beamLight && !externalPresentation) beamLight.enabled = active;
        if (beamAudio && !externalPresentation) { if (active && beamAudio.clip && !beamAudio.isPlaying) beamAudio.Play(); else if (!active) beamAudio.Stop(); }
    }
    private Transform interactionFocus;
    public void SetInteractionFocus(Transform focus) => interactionFocus = focus;
    private int lastConsumedBark = -1;
    private float lastBarkTime = float.NegativeInfinity;
    private Color displayedColor;
    private Vector3 originalScale;
    private MaterialPropertyBlock block, originalBlock;
    private float originalVolume, originalPitch, originalLightIntensity;
    private Color originalLightColor;
    public float Interference => interference;
    public AbductionBeamState CurrentState => currentState;

    private void Awake()
    {
        originalScale = transform.localScale;
        displayedColor = stableColor;
        block = new MaterialPropertyBlock(); originalBlock = new MaterialPropertyBlock();
        if (beamRenderer) beamRenderer.GetPropertyBlock(originalBlock);
        if (beamLight) { originalLightIntensity = beamLight.intensity; originalLightColor = beamLight.color; }
        if (beamAudio)
        {
            originalVolume = beamAudio.volume; originalPitch = beamAudio.pitch;
            if (!externalPresentation && beamActive && beamAudio.clip && !beamAudio.isPlaying) beamAudio.Play();
        }
    }

    private void Start() => SetBeamActive(beamActive);

    public void OnBark()
    {
        if (!IsBeamActive || !barkSource || barkSource.BarkCount == lastConsumedBark) return;
        lastConsumedBark = barkSource.BarkCount;
        Transform origin = barkSource.Origin;
        Vector3 target = interactionFocus ? interactionFocus.position : beamRenderer ? beamRenderer.bounds.ClosestPoint(origin.position) : transform.position;
        Vector3 offset = target - origin.position;
        if (offset.sqrMagnitude > barkRange * barkRange) return;
        // El perro gira sobre Y; la orientación se comprueba en el plano del piso.
        Vector3 flatDirection = Vector3.ProjectOnPlane(offset, Vector3.up);
        Vector3 flatForward = Vector3.ProjectOnPlane(origin.forward, Vector3.up);
        if (flatDirection.sqrMagnitude > 0.01f && Vector3.Angle(flatForward, flatDirection) > maximumFacingAngle) return;
        interference = Mathf.Clamp01(interference + barkSource.LastInterference);
        lastBarkTime = Time.time;
        RefreshState();
        onInterferenceAccepted.Invoke();
    }

    private void Update()
    {
        if (!beamActive) return;
        if (Time.time - lastBarkTime >= recoveryDelay)
            interference = Mathf.MoveTowards(interference, 0f, recoveryPerSecond * Time.deltaTime);
        RefreshState();
    }

    private void RefreshState()
    {
        var next = interference >= uncontrolledThreshold ? AbductionBeamState.Uncontrolled
            : interference >= intenseThreshold ? AbductionBeamState.IntenseInterference
            : interference >= affectedThreshold ? AbductionBeamState.Affected : AbductionBeamState.Stable;
        if (next == currentState) return;
        currentState = next;
        onStateChanged.Invoke(currentState);
        if (currentState == AbductionBeamState.Uncontrolled) onUncontrolled.Invoke();
    }

    private void LateUpdate()
    {
        if (!beamActive) return;
        if (externalPresentation)
        {
            // Se conserva el pulso del volumen lógico utilizado por ladridos y TV.
            float logicalOscillation = Mathf.Sin(Time.time * Mathf.Lerp(2f, 18f, interference));
            transform.localScale = originalScale * (1f + logicalOscillation * interference * pulseAmount);
            return;
        }
        Color target = currentState == AbductionBeamState.Uncontrolled ? uncontrolledColor
            : currentState == AbductionBeamState.IntenseInterference ? intenseColor
            : currentState == AbductionBeamState.Affected ? affectedColor : stableColor;
        float blend = 1f - Mathf.Exp(-feedbackResponse * Time.deltaTime);
        displayedColor = Color.Lerp(displayedColor, target, blend);
        float oscillation = Mathf.Sin(Time.time * Mathf.Lerp(2f, 18f, interference));
        if (beamRenderer)
        {
            beamRenderer.GetPropertyBlock(block); block.SetColor("_BaseColor", displayedColor);
            beamRenderer.SetPropertyBlock(block);
        }
        if (beamLight)
        {
            beamLight.color = displayedColor;
            float intensity = lightIntensity.Evaluate(interference) * (1f - interference * flickerAmount * (0.5f + 0.5f * oscillation));
            beamLight.intensity = Mathf.Lerp(beamLight.intensity, intensity, blend);
        }
        transform.localScale = originalScale * (1f + oscillation * interference * pulseAmount);
        if (beamAudio)
        {
            beamAudio.volume = Mathf.Lerp(beamAudio.volume, Mathf.Lerp(stableVolume, maximumVolume, interference), blend);
            beamAudio.pitch = Mathf.Lerp(beamAudio.pitch, Mathf.Lerp(pitchRange.x, pitchRange.y, interference) + oscillation * interference * 0.035f, blend);
        }
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        transform.localScale = originalScale;
        if (beamRenderer && originalBlock != null) beamRenderer.SetPropertyBlock(originalBlock);
        if (beamLight && !externalPresentation) { beamLight.intensity = originalLightIntensity; beamLight.color = originalLightColor; }
        if (beamAudio && !externalPresentation) { beamAudio.Stop(); beamAudio.volume = originalVolume; beamAudio.pitch = originalPitch; }
    }

    private void OnValidate()
    {
        affectedThreshold = Mathf.Clamp(affectedThreshold, 0.01f, 0.97f);
        intenseThreshold = Mathf.Clamp(intenseThreshold, affectedThreshold + 0.01f, 0.99f);
        uncontrolledThreshold = Mathf.Clamp(uncontrolledThreshold, intenseThreshold + 0.01f, 1f);
    }
}


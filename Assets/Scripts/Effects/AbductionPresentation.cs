using System;
using UnityEngine;

[Serializable]
public sealed class BeamPresentationStyle
{
    public Color color;
    [Min(0.1f)] public float pulseHz;
    [Range(0f, 0.7f)] public float irregularity;
    [Range(0f, 1.2f)] public float audioGain;
    [Range(0.5f, 1.5f)] public float pitch;
    [Range(500f, 12000f)] public float cutoff;
    public Texture2D texture;

    public BeamPresentationStyle(Color color, float pulseHz, float irregularity, float audioGain, float pitch, float cutoff)
    {
        this.color = color; this.pulseHz = pulseHz; this.irregularity = irregularity;
        this.audioGain = audioGain; this.pitch = pitch; this.cutoff = cutoff;
    }
}

// Presentación local: observa estados confirmados y el foco exterior existente.
// No lee Input, calcula interferencia, mueve dueños ni avanza episodios.
[DefaultExecutionOrder(100)]
public sealed class AbductionPresentation : MonoBehaviour
{
    [Header("Referencias existentes")]
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private WindowAmbienceFocus windowFocus;
    [SerializeField] private Renderer logicalVolume;
    [SerializeField] private Transform owners;
    [SerializeField] private BedroomShutter bedroomShutter;
    [SerializeField] private Transform bedroomWindow, functionalWindow, blockedWindow;
    [SerializeField] private Light ownerGlow, functionalWindowLight, blockedWindowLight;
    [SerializeField] private AudioSource beamAudio;
    [SerializeField] private AudioLowPassFilter beamFilter;
    [Header("Nave y enlace visual")]
    [SerializeField] private Transform shipVisual, shipEmitter;
    [SerializeField] private Renderer[] shipRenderers;
    [SerializeField] private Light shipLight, bedroomWindowLight;
    [SerializeField] private LineRenderer exteriorBeam, interiorBeam;
    [SerializeField] private Vector3 ownerTargetOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField, Min(0.05f)] private float exteriorWidth = 0.55f, interiorWidth = 0.7f;
    [Header("Movimiento y transiciones")]
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.45f;
    [SerializeField, Min(0f)] private float floatAmplitude = 0.065f;
    [SerializeField, Min(0.05f)] private float floatSpeed = 0.65f;
    [SerializeField, Range(0f, 0.3f)] private float interferenceMotion = 0.1f;
    [SerializeField, Range(0f, 5f)] private float maximumTilt = 2f;
    [Header("Iluminación y feedback")]
    [SerializeField, Min(0f)] private float ownerGlowIntensity = 2.2f;
    [SerializeField, Min(0f)] private float windowIntensity = 2.6f, shipLightIntensity = 1.5f;
    [SerializeField, Min(0.1f)] private float barkFeedbackSeconds = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float barkFeedbackGain = 0.25f;
    [Header("Estados: mismo orden que AbductionBeamState")]
    [SerializeField] private BeamPresentationStyle[] styles =
    {
        new BeamPresentationStyle(new Color(0.2f, 0.85f, 1f, 0.45f), 0.45f, 0f, 1f, 1f, 6500f),
        new BeamPresentationStyle(new Color(0.25f, 1f, 0.8f, 0.5f), 0.8f, 0.14f, 0.95f, 1.06f, 4200f),
        new BeamPresentationStyle(new Color(0.15f, 0.95f, 0.9f, 0.55f), 1.25f, 0.36f, 1.05f, 1.15f, 2800f),
        new BeamPresentationStyle(new Color(0.35f, 1f, 0.7f, 0.6f), 1.7f, 0.6f, 0.95f, 1.22f, 1800f)
    };

    private MaterialPropertyBlock shipBlock, beamBlock;
    private Color[] hullColors, hullEmission;
    private float originalVolume, originalPitch, originalCutoff;
    private bool originalForceRenderingOff;
    private float visibility, acceptedBarkAt = float.NegativeInfinity;
    private Transform currentWindow;
    private bool currentBlocked, currentBedroom, ready;
    private Color displayedColor;
    private Light[] existingLights;
    private Color[] originalLightColors;
    private float[] originalLightIntensities;

    private void Awake()
    {
        if (!beam || !windowFocus || !logicalVolume || !owners || !bedroomShutter ||
            !bedroomWindow || !functionalWindow || !blockedWindow || !shipVisual || !shipEmitter ||
            !exteriorBeam || !interiorBeam || !beamAudio || !beamFilter || !ownerGlow ||
            !functionalWindowLight || !blockedWindowLight || !shipLight || !bedroomWindowLight ||
            styles == null || styles.Length != 4 || shipRenderers == null || shipRenderers.Length == 0)
        {
            Debug.LogError("Abduction presentation: incomplete references.", this);
            enabled = false;
            return;
        }
        shipBlock = new MaterialPropertyBlock(); beamBlock = new MaterialPropertyBlock();
        originalVolume = beamAudio.volume; originalPitch = beamAudio.pitch; originalCutoff = beamFilter.cutoffFrequency;
        originalForceRenderingOff = logicalVolume.forceRenderingOff;
        // enabled y bounds siguen disponibles para ladridos y TV; solo se omite el dibujo.
        logicalVolume.forceRenderingOff = true;
        hullColors = new Color[shipRenderers.Length]; hullEmission = new Color[shipRenderers.Length];
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            hullColors[i] = shipRenderers[i].sharedMaterial.GetColor("_BaseColor");
            hullEmission[i] = shipRenderers[i].sharedMaterial.GetColor("_EmissionColor");
        }
        existingLights = new[] { ownerGlow, functionalWindowLight, blockedWindowLight };
        originalLightColors = new Color[existingLights.Length]; originalLightIntensities = new float[existingLights.Length];
        for (int i = 0; i < existingLights.Length; i++)
        { originalLightColors[i] = existingLights[i].color; originalLightIntensities[i] = existingLights[i].intensity; }
        currentWindow = bedroomWindow; displayedColor = styles[(int)beam.CurrentState].color;
        ready = true;
        ApplyShipFade(0f, 1f);
    }

    private void OnEnable()
    {
        if (beam) beam.InterferenceAccepted.AddListener(OnInterferenceAccepted);
    }

    private void OnInterferenceAccepted() => acceptedBarkAt = Time.time;

    private void LateUpdate()
    {
        if (!ready) return;
        bool active = beam.IsBeamActive;
        bool focused = active && windowFocus.HasAbductionFocus;
        if (focused)
        {
            currentBedroom = windowFocus.IsBedroomFocus; currentBlocked = windowFocus.IsBlockedFocus;
            currentWindow = currentBedroom ? bedroomWindow : currentBlocked ? blockedWindow : functionalWindow;
        }
        visibility = Mathf.MoveTowards(visibility, focused ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        float weight = visibility * windowFocus.ExteriorVisibility;
        var style = styles[(int)beam.CurrentState];
        float blend = 1f - Mathf.Exp(-8f * Time.deltaTime);
        displayedColor = Color.Lerp(displayedColor, style.color, blend);
        float noise = Mathf.PerlinNoise(Time.time * 1.4f, 2.7f);
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * style.pulseHz * Mathf.PI * 2f + noise * style.irregularity * 3f);
        float feedback = Mathf.Clamp01(1f - (Time.time - acceptedBarkAt) / Mathf.Max(0.1f, barkFeedbackSeconds));
        float energy = Mathf.Clamp(0.88f + wave * 0.12f - noise * style.irregularity * 0.55f + feedback * barkFeedbackGain, 0.35f, 1.2f);
        if (windowFocus.ShipOrigin)
        {
            float irregular = style.irregularity * interferenceMotion;
            shipVisual.position = windowFocus.ShipOrigin.position + new Vector3(
                (noise - 0.5f) * irregular,
                Mathf.Sin(Time.time * floatSpeed) * floatAmplitude + Mathf.Sin(Time.time * 2.1f) * irregular, 0f);
            shipVisual.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.1f) * style.irregularity * maximumTilt,
                Mathf.Sin(Time.time * 0.18f) * 5f, Mathf.Sin(Time.time * 0.9f) * style.irregularity * maximumTilt);
        }
        ApplyShipFade(weight, energy);
        Vector3 outward = currentWindow.forward;
        Vector3 entry = currentWindow.position;
        DrawBeam(exteriorBeam, shipEmitter.position, entry + outward * 0.08f, exteriorWidth, weight, energy, style);
        bool open = !currentBlocked && (!currentBedroom || !bedroomShutter.IsClosed);
        Vector3 insideStart = entry - outward * 0.23f;
        Vector3 insideEnd = owners.position + ownerTargetOffset;
        // Solo recorta la representación si encuentra una superficie. No altera detecciones.
        if (focused && open && Physics.Linecast(insideStart, insideEnd, out RaycastHit obstruction, ~0, QueryTriggerInteraction.Ignore)
            && !obstruction.transform.IsChildOf(owners) && !obstruction.collider.GetComponentInParent<CharacterController>())
            insideEnd = obstruction.point;
        DrawBeam(interiorBeam, insideStart, insideEnd, interiorWidth, focused && open ? weight : 0f, energy, style);
        ownerGlow.enabled = active;
        ownerGlow.color = displayedColor; ownerGlow.intensity = ownerGlowIntensity * energy * (active ? 1f : 0f);
        bedroomWindowLight.enabled = focused && currentBedroom && open;
        if (bedroomWindowLight.enabled) AimWindowLight(bedroomWindowLight, insideStart, insideEnd, energy * weight);
        if (functionalWindowLight.enabled) AimWindowLight(functionalWindowLight, functionalWindow.position - functionalWindow.forward * 0.23f,
            owners.position + ownerTargetOffset, energy * weight);
        // La ventana bloqueada conserva un resplandor local; no atraviesa el panel con un Spot.
        blockedWindowLight.color = displayedColor;
        blockedWindowLight.intensity = windowIntensity * energy * weight;
        shipLight.enabled = weight > 0.01f;
        shipLight.color = displayedColor; shipLight.intensity = shipLightIntensity * energy * weight;
        UpdateAudio(active, style, wave, noise, feedback, blend);
    }

    private void ApplyShipFade(float weight, float energy)
    {
        for (int i = 0; i < shipRenderers.Length; i++)
        {
            var r = shipRenderers[i]; r.enabled = weight > 0.005f;
            var color = hullColors[i]; color.a *= weight;
            shipBlock.Clear(); shipBlock.SetColor("_BaseColor", color);
            shipBlock.SetColor("_EmissionColor", hullEmission[i] * energy * weight);
            r.SetPropertyBlock(shipBlock);
        }
    }

    private void DrawBeam(LineRenderer line, Vector3 from, Vector3 to, float width, float weight, float energy, BeamPresentationStyle style)
    {
        line.enabled = weight > 0.005f;
        if (!line.enabled) return;
        line.SetPosition(0, from); line.SetPosition(1, to);
        line.startWidth = width * 0.5f; line.endWidth = width * (1f + style.irregularity * 0.12f);
        var color = displayedColor; color.a *= weight * energy;
        line.startColor = color; line.endColor = color;
        beamBlock.Clear(); beamBlock.SetTexture("_BaseMap", style.texture);
        beamBlock.SetVector("_BaseMap_ST", new Vector4(1f, 1f, -Time.time * (0.08f + style.irregularity * 0.2f), 0f));
        line.SetPropertyBlock(beamBlock);
    }

    private void AimWindowLight(Light light, Vector3 from, Vector3 to, float gain)
    {
        light.transform.position = from;
        if ((to - from).sqrMagnitude > 0.01f) light.transform.rotation = Quaternion.LookRotation(to - from);
        light.color = displayedColor; light.intensity = windowIntensity * gain;
    }

    private void UpdateAudio(bool active, BeamPresentationStyle style, float wave, float noise, float feedback, float blend)
    {
        if (active && beamAudio.clip && !beamAudio.isPlaying) beamAudio.Play();
        else if (!active && beamAudio.isPlaying) beamAudio.Stop();
        float modulation = 1f - style.irregularity * (0.3f * wave + 0.4f * noise);
        if (beam.CurrentState == AbductionBeamState.Uncontrolled)
            modulation *= Mathf.Lerp(0.18f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.65f, wave)));
        float target = active ? Mathf.Min(0.8f, originalVolume * style.audioGain * modulation) : 0f;
        beamAudio.volume = Mathf.Lerp(beamAudio.volume, target, blend);
        beamAudio.pitch = Mathf.Lerp(beamAudio.pitch, style.pitch + style.irregularity * (noise - 0.5f) * 0.13f + feedback * 0.025f, blend);
        beamFilter.cutoffFrequency = Mathf.Lerp(beamFilter.cutoffFrequency, style.cutoff * (1f - style.irregularity * wave * 0.35f), blend);
    }

    private void OnDisable()
    {
        if (beam) beam.InterferenceAccepted.RemoveListener(OnInterferenceAccepted);
        if (!ready) return;
        logicalVolume.forceRenderingOff = originalForceRenderingOff;
        foreach (var r in shipRenderers) { r.SetPropertyBlock(null); r.enabled = false; }
        exteriorBeam.enabled = false; interiorBeam.enabled = false;
        bedroomWindowLight.enabled = false; shipLight.enabled = false;
        for (int i = 0; i < existingLights.Length; i++)
        { existingLights[i].color = originalLightColors[i]; existingLights[i].intensity = originalLightIntensities[i]; }
        beamAudio.Stop(); beamAudio.volume = originalVolume; beamAudio.pitch = originalPitch;
        beamFilter.cutoffFrequency = originalCutoff;
    }
}

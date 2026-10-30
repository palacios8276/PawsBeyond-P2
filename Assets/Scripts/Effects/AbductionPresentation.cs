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
    [Range(0f, 0.9f)] public float pulseDepth = 0.08f;
    [Range(0f, 0.6f)] public float widthVariation = 0.02f;
    [Range(0f, 0.25f)] public float lateralWobble;

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
    [SerializeField] private Transform bathroomWindow;
    [SerializeField] private Light ownerGlow, functionalWindowLight, blockedWindowLight;
    [SerializeField] private AudioSource beamAudio;
    [SerializeField] private AudioLowPassFilter beamFilter;
    [Header("Nave y enlace visual")]
    [SerializeField] private Transform shipVisual, shipEmitter;
    [SerializeField] private Renderer[] shipRenderers;
    [SerializeField] private Light shipLight, bedroomWindowLight;
    [SerializeField] private LineRenderer exteriorBeam, interiorBeam;
    [SerializeField] private Vector3 ownerTargetOffset = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private Vector3 projectionTargetOffset = new Vector3(0f, -0.7f, 0f);
    [SerializeField, Min(0.05f)] private float exteriorWidth = 0.55f, interiorWidth = 1.15f;
    [SerializeField, Range(1f, 1.5f)] private float interiorPresence = 1.2f;
    [Header("Energía de acompañamiento, sin conexión con ventanas")]
    [SerializeField] private LineRenderer accompanyingBeam;
    [SerializeField] private Vector3 accompanyingOffset = new Vector3(0f, 0.2f, 0f);
    [SerializeField, Min(0.2f)] private float accompanyingHeight = 2.2f, accompanyingWidth = 2.3f;
    [SerializeField, Range(0f, 1f)] private float accompanyingOpacity = 0.6f;
    [Header("Movimiento y transiciones")]
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.45f;
    [SerializeField, Min(0f)] private float floatAmplitude = 0.065f;
    [SerializeField, Min(0.05f)] private float floatSpeed = 0.65f;
    [SerializeField, Range(0f, 0.3f)] private float interferenceMotion = 0.1f;
    [SerializeField, Range(0f, 5f)] private float maximumTilt = 2f;
    [Header("Iluminación y feedback")]
    [SerializeField, Min(0f)] private float ownerGlowIntensity = 6f;
    [SerializeField, Min(0f)] private float windowIntensity = 5f, shipLightIntensity = 1.5f;
    [SerializeField, Min(0.1f)] private float barkFeedbackSeconds = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float barkFeedbackGain = 0.45f;
    [SerializeField, Range(0f, 0.8f)] private float barkWidthKick = 0.55f;
    [SerializeField, Range(0f, 0.2f)] private float barkLateralKick = 0.12f;
    [SerializeField] private Color barkResponseColor = new Color(1f, 0.85f, 0.45f, 0.6f);
    [Header("Estados: mismo orden que AbductionBeamState")]
    [SerializeField] private BeamPresentationStyle[] styles =
    {
        new BeamPresentationStyle(new Color(0.2f, 0.85f, 1f, 0.45f), 0.45f, 0f, 1f, 1f, 6500f)
            { pulseDepth = 0.06f, widthVariation = 0.02f, lateralWobble = 0f },
        new BeamPresentationStyle(new Color(0.25f, 1f, 0.55f, 0.5f), 0.8f, 0.14f, 0.95f, 1.06f, 4200f)
            { pulseDepth = 0.32f, widthVariation = 0.12f, lateralWobble = 0.035f },
        new BeamPresentationStyle(new Color(1f, 0.58f, 0.12f, 0.55f), 1.25f, 0.36f, 1.05f, 1.15f, 2800f)
            { pulseDepth = 0.58f, widthVariation = 0.27f, lateralWobble = 0.1f },
        new BeamPresentationStyle(new Color(0.85f, 0.22f, 1f, 0.6f), 1.7f, 0.6f, 0.95f, 1.22f, 1800f)
            { pulseDepth = 0.84f, widthVariation = 0.42f, lateralWobble = 0.2f }
    };

    private MaterialPropertyBlock shipBlock, beamBlock;
    private Color[] hullColors, hullEmission;
    private float originalVolume, originalPitch, originalCutoff;
    private bool originalForceRenderingOff;
    private float visibility, acceptedBarkAt = float.NegativeInfinity;
    private float accompanyingVisibility;
    private Transform currentWindow;
    private bool currentBlocked, currentBedroom, currentBathroom, currentKitchen, ready;
    private Color displayedColor;
    private Light[] existingLights;
    private Color[] originalLightColors;
    private float[] originalLightIntensities;

    private void Awake()
    {
        if (!beam || !windowFocus || !logicalVolume || !owners || !bedroomShutter ||
            !bedroomWindow || !functionalWindow || !blockedWindow || !bathroomWindow || !shipVisual || !shipEmitter ||
            !exteriorBeam || !interiorBeam || !accompanyingBeam || !beamAudio || !beamFilter || !ownerGlow ||
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

    public float BarkFeedback => beam && beam.IsBeamActive
        ? Mathf.Clamp01(1f - (Time.time - acceptedBarkAt) / Mathf.Max(0.1f, barkFeedbackSeconds)) : 0f;
    private void OnInterferenceAccepted() => acceptedBarkAt = Time.time;

    private void LateUpdate()
    {
        if (!ready) return;
        bool active = beam.IsBeamActive;
        bool focused = active && windowFocus.HasAbductionFocus;
        if (focused)
        {
            currentBedroom = windowFocus.IsBedroomFocus; currentBlocked = windowFocus.IsBlockedFocus;
            currentBathroom = windowFocus.IsBathroomFocus;
            currentKitchen = windowFocus.IsKitchenFocus;
            currentWindow = currentKitchen ? windowFocus.ActiveKitchenWindow
                : currentBathroom ? bathroomWindow : currentBedroom ? bedroomWindow : currentBlocked ? blockedWindow : functionalWindow;
        }
        visibility = Mathf.MoveTowards(visibility, focused ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        float weight = visibility * windowFocus.ExteriorVisibility;
        var style = styles[(int)beam.CurrentState];
        float blend = 1f - Mathf.Exp(-8f * Time.deltaTime);
        displayedColor = Color.Lerp(displayedColor, style.color, blend);
        float noise = Mathf.PerlinNoise(Time.time * 1.4f, 2.7f);
        float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * style.pulseHz * Mathf.PI * 2f + noise * style.irregularity * 3f);
        float feedback = BarkFeedback;
        // La envolvente visual es independiente del audio y de la interferencia jugable.
        float energy = Mathf.Clamp(1f - style.pulseDepth * (1f - wave) - noise * style.irregularity * 0.1f
            + feedback * barkFeedbackGain, 0.18f, 1.4f);
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
        DrawBeam(exteriorBeam, shipEmitter.position, entry + outward * 0.08f, exteriorWidth, weight, energy, style, false, wave, feedback);
        bool open = !currentBlocked && (!currentBedroom || !bedroomShutter.IsClosed)
            && (!currentKitchen || !windowFocus.IsKitchenConnectionClosed);
        Vector3 insideStart = entry - outward * 0.23f;
        Vector3 insideEnd = owners.position + ownerTargetOffset;
        // Solo recorta la representación si encuentra una superficie. No altera detecciones.
        if (focused && open && Physics.Linecast(insideStart, insideEnd, out RaycastHit obstruction, ~0, QueryTriggerInteraction.Ignore)
            && !obstruction.transform.IsChildOf(owners) && !obstruction.collider.GetComponentInParent<CharacterController>())
            insideEnd = obstruction.point;
        DrawBeam(interiorBeam, insideStart, insideEnd, interiorWidth, focused && open ? weight : 0f, energy, style, true, wave, feedback);
        accompanyingVisibility = Mathf.MoveTowards(accompanyingVisibility, active ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.05f, fadeSeconds));
        // Relevo visual complementario: acompaña también pausas y ventanas cerradas.
        // No utiliza el fundido exterior para desaparecer durante un cambio de sector.
        float connectedWeight = focused && open ? weight : 0f;
        DrawAccompanyingBeam(accompanyingVisibility * (1f - connectedWeight), wave, feedback);
        ownerGlow.enabled = active;
        ownerGlow.color = displayedColor; ownerGlow.intensity = ownerGlowIntensity * energy * (active ? 1f : 0f);
        bedroomWindowLight.enabled = focused && (currentBedroom || currentBathroom || currentKitchen) && open;
        Vector3 projectionTarget = owners.position + projectionTargetOffset;
        if (bedroomWindowLight.enabled) AimWindowLight(bedroomWindowLight, insideStart, projectionTarget, energy * weight);
        if (functionalWindowLight.enabled) AimWindowLight(functionalWindowLight, functionalWindow.position - functionalWindow.forward * 0.23f,
            projectionTarget, energy * weight);
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

    private void DrawBeam(LineRenderer line, Vector3 from, Vector3 to, float width, float weight, float energy,
        BeamPresentationStyle style, bool interior, float wave, float feedback)
    {
        line.enabled = weight > 0.005f;
        if (!line.enabled) return;
        int points = interior ? 4 : 2;
        if (line.positionCount != points) line.positionCount = points;
        line.SetPosition(0, from); line.SetPosition(points - 1, to);
        if (interior)
        {
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up).normalized;
            float wobble = (Mathf.PerlinNoise(Time.time * 1.7f, 6.2f) * 2f - 1f) * style.lateralWobble;
            wobble += feedback * barkLateralKick * Mathf.Lerp(0.75f, 1.25f, (int)beam.CurrentState / 3f);
            line.SetPosition(1, Vector3.Lerp(from, to, 0.28f) + side * wobble * 0.4f);
            line.SetPosition(2, Vector3.Lerp(from, to, 0.68f) - side * wobble);
        }
        // La boca de la ventana permanece estrecha; el extremo interior expresa la inestabilidad.
        line.startWidth = interior ? 0.32f : width * 0.5f;
        line.endWidth = interior ? width * (1f + style.widthVariation * (wave * 2f - 1f) + feedback * barkWidthKick) : width;
        var color = Color.Lerp(displayedColor, barkResponseColor, feedback * 0.45f);
        color.a = Mathf.Min(0.78f, color.a * weight * energy * (interior ? interiorPresence : 1f));
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

    private void DrawAccompanyingBeam(float weight, float wave, float feedback)
    {
        accompanyingBeam.enabled = weight > 0.005f;
        if (!accompanyingBeam.enabled) return;
        if (accompanyingBeam.positionCount != 5) accompanyingBeam.positionCount = 5;
        Vector3 center = owners.position + accompanyingOffset;
        // Volumen contenido y recto: no apunta al techo, no tira de los dueños y no zigzaguea en el baño.
        for (int i = 0; i < 5; i++)
            accompanyingBeam.SetPosition(i, center + Vector3.up * accompanyingHeight * (0.5f - i / 4f));
        accompanyingBeam.widthMultiplier = accompanyingWidth * (1f + feedback * barkWidthKick * 0.45f);
        Color color = Color.Lerp(displayedColor, barkResponseColor, feedback * 0.45f);
        color.a *= weight * accompanyingOpacity * (0.92f + wave * 0.08f + feedback * 0.12f);
        beamBlock.Clear(); beamBlock.SetColor("_BaseColor", color);
        // Conserva una base continua entre acontecimientos, aun si el estado crítico fragmenta el enlace de ventana.
        beamBlock.SetTexture("_BaseMap", styles[(int)AbductionBeamState.Stable].texture);
        beamBlock.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
        accompanyingBeam.SetPropertyBlock(beamBlock);
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
        if (logicalVolume) logicalVolume.forceRenderingOff = originalForceRenderingOff;
        foreach (var r in shipRenderers) if (r) { r.SetPropertyBlock(null); r.enabled = false; }
        if (exteriorBeam) exteriorBeam.enabled = false;
        if (interiorBeam) interiorBeam.enabled = false;
        if (accompanyingBeam) { accompanyingBeam.enabled = false; accompanyingBeam.SetPropertyBlock(null); }
        if (bedroomWindowLight) bedroomWindowLight.enabled = false;
        if (shipLight) shipLight.enabled = false;
        for (int i = 0; i < existingLights.Length; i++)
        { if (existingLights[i]) { existingLights[i].color = originalLightColors[i]; existingLights[i].intensity = originalLightIntensities[i]; } }
        if (beamAudio) { beamAudio.Stop(); beamAudio.volume = originalVolume; beamAudio.pitch = originalPitch; }
        if (beamFilter) beamFilter.cutoffFrequency = originalCutoff;
    }
}

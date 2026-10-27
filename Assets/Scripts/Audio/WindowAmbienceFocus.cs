using UnityEngine;

// Mezcla local de ventanas. Volúmenes base y atenuación se editan en los AudioSources.
public sealed class WindowAmbienceFocus : MonoBehaviour
{
    [SerializeField] private AudioSource nightAmbience, windowWind, shipAmbience;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private Transform functionalExterior, blockedExterior, neutralExterior;
    [SerializeField] private Transform bedroomExterior;
    [SerializeField] private Transform bathroomExterior;
    [SerializeField, Min(0.05f)] private float transitionSeconds = 0.8f;
    [SerializeField, Min(0.05f)] private float contextFadeSeconds = 0.6f;
    [SerializeField, Range(0f, 1f)] private float bedroomNatureGain = 0.9f;
    [SerializeField, Min(1f)] private float bedroomShipGain = 2.2f;
    [SerializeField, Range(0f, 1f)] private float focusedNatureGain = 0.8f;
    [SerializeField, Min(1f)] private float focusedShipGain = 2.4f;
    [SerializeField, Range(0f, 1f)] private float shiftedNatureGain = 0.65f;
    [SerializeField, Min(1f)] private float shiftedShipGain = 3.4f;
    [SerializeField, Min(1f)] private float activeBeamShipGain = 1f;
    [SerializeField] private Vector3 shipOffset = new Vector3(0f, 3f, 5f);
    [SerializeField] private Vector3 bathroomShipOffset = new Vector3(0f, 0.5f, 4.25f);
    private float nightVolume, windVolume, shipVolume;
    private float natureGain = 1f, shipGain = 1f, contextWeight = 1f;
    private Transform focus;
    private bool windowActive, focusShifted, bedroomActive;
    private bool bedroomStarted, livingStarted, relocating;
    private bool bathroomActive, bathroomStarted;

    // Lectura para la presentación: conserva una sola autoridad sobre foco y transición.
    public bool HasAbductionFocus => bedroomActive || windowActive || bathroomActive;
    public bool IsBathroomFocus => bathroomActive;
    public bool IsBedroomFocus => bedroomActive;
    public bool IsBlockedFocus => windowActive && focusShifted;
    public float ExteriorVisibility => contextWeight;
    public Transform ShipOrigin => shipAmbience ? shipAmbience.transform : null;

    private void Awake()
    {
        nightVolume = nightAmbience.volume; windVolume = windowWind.volume; shipVolume = shipAmbience.volume;
        focus = neutralExterior;
    }

    private void Start()
    {
        foreach (var source in new[] { nightAmbience, windowWind, shipAmbience })
            if (source.clip && !source.isPlaying) source.Play();
    }

    public void BeginBedroomAbduction()
    {
        if (bedroomStarted || livingStarted || !bedroomExterior) return;
        bedroomStarted = true; bedroomActive = true;
        SwitchExteriorContext(bedroomExterior);
    }

    public void EndBedroomAbduction()
    {
        if (!bedroomActive || livingStarted) return;
        bedroomActive = false;
        SwitchExteriorContext(neutralExterior);
    }

    private void SwitchExteriorContext(Transform target)
    {
        focus = target;
        // Entre sectores, primero silencio: no se simula un vuelo a través de la casa.
        relocating = true;
    }

    public void BeginWindowAttempt()
    {
        if (livingStarted) return;
        livingStarted = true; bedroomActive = false;
        windowActive = true; focusShifted = false;
        if (focus == bedroomExterior) SwitchExteriorContext(functionalExterior);
        else focus = functionalExterior;
    }

    public void ShiftToBlockedWindow()
    {
        if (!windowActive || focusShifted) return;
        focusShifted = true; focus = blockedExterior;
    }

    public void EndWindowAttempt()
    {
        if (!windowActive) return;
        windowActive = false; focus = neutralExterior;
    }

    public void BeginBathroomAbduction()
    {
        if (bathroomStarted || !bathroomExterior) return;
        bathroomStarted = true; bathroomActive = true;
        bedroomActive = false; windowActive = false;
        SwitchExteriorContext(bathroomExterior);
    }

    public void EndBathroomAbduction()
    {
        if (!bathroomActive) return;
        bathroomActive = false;
        SwitchExteriorContext(neutralExterior);
    }

    private void Update()
    {
        float naturalTarget = bathroomActive ? focusedNatureGain : bedroomActive ? bedroomNatureGain
            : windowActive ? (focusShifted ? shiftedNatureGain : focusedNatureGain) : 1f;
        float shipTarget = bathroomActive ? focusedShipGain : bedroomActive ? bedroomShipGain
            : windowActive ? (focusShifted ? shiftedShipGain : focusedShipGain)
            : (beam && beam.IsBeamActive ? activeBeamShipGain : 1f);
        float blend = 1f - Mathf.Exp(-3f * Time.deltaTime / transitionSeconds);
        natureGain = Mathf.Lerp(natureGain, naturalTarget, blend);
        shipGain = Mathf.Lerp(shipGain, shipTarget, blend);
        contextWeight = Mathf.MoveTowards(contextWeight, relocating ? 0f : 1f, Time.deltaTime / contextFadeSeconds);
        Transform target = focus ? focus : neutralExterior;
        Vector3 targetShipOffset = target == bathroomExterior ? bathroomShipOffset : shipOffset;
        if (relocating && contextWeight <= 0f)
        {
            windowWind.transform.position = target.position;
            shipAmbience.transform.position = target.position + target.rotation * targetShipOffset;
            relocating = false;
        }
        else if (!relocating)
        {
            // Las dos ventanas de sala comparten fachada: aquí sí hay desplazamiento exterior.
            windowWind.transform.position = Vector3.Lerp(windowWind.transform.position, target.position, blend);
            shipAmbience.transform.position = Vector3.Lerp(shipAmbience.transform.position, target.position + target.rotation * targetShipOffset, blend);
        }
        nightAmbience.volume = nightVolume * natureGain;
        windowWind.volume = windVolume * natureGain * contextWeight;
        shipAmbience.volume = Mathf.Clamp01(shipVolume * shipGain * contextWeight);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (nightAmbience) nightAmbience.volume = nightVolume;
        if (windowWind) windowWind.volume = windVolume;
        if (shipAmbience) shipAmbience.volume = shipVolume;
    }
}

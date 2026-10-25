using UnityEngine;

// Mezcla local del episodio: reutiliza ambientes y deja el emisor del rayo independiente.
public sealed class WindowAmbienceFocus : MonoBehaviour
{
    [SerializeField] private AudioSource nightAmbience, windowWind, shipAmbience;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private Transform functionalExterior, blockedExterior, neutralExterior;
    [SerializeField, Min(0.05f)] private float transitionSeconds = 0.8f;
    [SerializeField, Range(0f, 1f)] private float focusedNatureGain = 0.4f;
    [SerializeField, Min(1f)] private float focusedShipGain = 2.4f;
    [SerializeField, Min(1f)] private float activeBeamShipGain = 1.3f;
    [SerializeField] private Vector3 shipOffset = new Vector3(0f, 3f, 5f);
    private float nightVolume, windVolume, shipVolume, focusWeight;
    private Transform focus;
    private bool windowActive;

    private void Awake()
    {
        nightVolume = nightAmbience.volume; windVolume = windowWind.volume; shipVolume = shipAmbience.volume;
    }

    private void Start()
    {
        foreach (var source in new[] { nightAmbience, windowWind, shipAmbience })
            if (source.clip && !source.isPlaying) source.Play();
    }

    public void BeginWindowAttempt() { windowActive = true; focus = functionalExterior; }
    public void ShiftToBlockedWindow() { windowActive = true; focus = blockedExterior; }
    public void EndWindowAttempt() { windowActive = false; }

    private void Update()
    {
        focusWeight = Mathf.MoveTowards(focusWeight, windowActive ? 1f : 0f, Time.deltaTime / transitionSeconds);
        float natureGain = Mathf.Lerp(1f, focusedNatureGain, focusWeight);
        nightAmbience.volume = nightVolume * natureGain; windowWind.volume = windVolume * natureGain;
        float beamGain = beam && beam.IsBeamActive ? activeBeamShipGain : 1f;
        shipAmbience.volume = Mathf.Clamp01(shipVolume * Mathf.Lerp(beamGain, focusedShipGain, focusWeight));
        Transform target = windowActive && focus ? focus : neutralExterior;
        float blend = 1f - Mathf.Exp(-4f * Time.deltaTime / transitionSeconds);
        windowWind.transform.position = Vector3.Lerp(windowWind.transform.position, target.position, blend);
        shipAmbience.transform.position = Vector3.Lerp(shipAmbience.transform.position, target.position + shipOffset, blend);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (nightAmbience) nightAmbience.volume = nightVolume;
        if (windowWind) windowWind.volume = windVolume;
        if (shipAmbience) shipAmbience.volume = shipVolume;
    }
}


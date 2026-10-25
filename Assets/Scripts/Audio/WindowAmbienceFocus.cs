using UnityEngine;

// Mezcla local del episodio. Los volúmenes base se editan en los AudioSources existentes.
public sealed class WindowAmbienceFocus : MonoBehaviour
{
    [SerializeField] private AudioSource nightAmbience, windowWind, shipAmbience;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private Transform functionalExterior, blockedExterior, neutralExterior;
    [SerializeField, Min(0.05f)] private float transitionSeconds = 0.8f;
    [SerializeField, Range(0f, 1f)] private float focusedNatureGain = 0.8f;
    [SerializeField, Min(1f)] private float focusedShipGain = 2.4f;
    [SerializeField, Range(0f, 1f)] private float shiftedNatureGain = 0.65f;
    [SerializeField, Min(1f)] private float shiftedShipGain = 3.4f;
    [SerializeField, Min(1f)] private float activeBeamShipGain = 1f;
    [SerializeField] private Vector3 shipOffset = new Vector3(0f, 3f, 5f);
    private float nightVolume, windVolume, shipVolume;
    private float natureGain = 1f, shipGain = 1f;
    private Transform focus;
    private bool windowActive, focusShifted;

    private void Awake()
    {
        nightVolume = nightAmbience.volume; windVolume = windowWind.volume; shipVolume = shipAmbience.volume;
    }

    private void Start()
    {
        foreach (var source in new[] { nightAmbience, windowWind, shipAmbience })
            if (source.clip && !source.isPlaying) source.Play();
    }

    public void BeginWindowAttempt()
    {
        if (windowActive) return;
        windowActive = true; focusShifted = false; focus = functionalExterior;
    }

    public void ShiftToBlockedWindow()
    {
        windowActive = true; focusShifted = true; focus = blockedExterior;
    }

    public void EndWindowAttempt() => windowActive = false;

    private void Update()
    {
        float naturalTarget = windowActive ? (focusShifted ? shiftedNatureGain : focusedNatureGain) : 1f;
        float shipTarget = windowActive ? (focusShifted ? shiftedShipGain : focusedShipGain)
            : (beam && beam.IsBeamActive ? activeBeamShipGain : 1f);
        float blend = 1f - Mathf.Exp(-3f * Time.deltaTime / transitionSeconds);
        natureGain = Mathf.Lerp(natureGain, naturalTarget, blend);
        shipGain = Mathf.Lerp(shipGain, shipTarget, blend);
        nightAmbience.volume = nightVolume * natureGain; windowWind.volume = windVolume * natureGain;
        shipAmbience.volume = Mathf.Clamp01(shipVolume * shipGain);
        Transform target = windowActive && focus ? focus : neutralExterior;
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

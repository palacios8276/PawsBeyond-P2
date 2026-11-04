using System.Collections;
using UnityEngine;

// Playlist diegética: una sola fuente, sin superposición ni reinicio al cambiar la tensión.
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class TVNewsController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AdaptiveMusicController musicState;
    [SerializeField] private AudioClip[] playlist;
    [Header("Niveles por noticia")]
    [SerializeField, Range(0f, 1f)] private float basePlaybackVolume = 0.3f;
    [Tooltip("Multiplicadores alineados con la playlist H/M/H/M.")]
    [SerializeField] private float[] clipVolumeMultipliers = { 1f, 0.52f, 0.55f, 1.01f };
    [SerializeField, Min(0.02f)] private float volumeTransitionSeconds = 0.15f;
    [Header("Transmisión normal")]
    [SerializeField, Range(0.5f, 2f)] private float regularPitch = 1f;
    [SerializeField, Min(0f)] private float regularPauseSeconds = 1.5f;
    [Header("Transmisión en peligro")]
    [SerializeField, Range(0.5f, 2f)] private float stressPitch = 1.15f;
    [SerializeField, Min(0f)] private float stressPauseSeconds = 0f;
    [SerializeField, Min(0.05f)] private float pitchTransitionSeconds = 0.45f;

    private Coroutine playlistRoutine;
    private float targetPitch = 1f;
    private float currentClipGain = 1f;
    private float occlusionMultiplier = 1f;
    private float volumeVelocity;
    private float remainingPause;
    private int nextIndex;
    private bool dangerActive;

    public int PlayedCount { get; private set; }
    public int NextIndex => nextIndex;
    public bool DangerActive => dangerActive;
    public AudioClip CurrentClip => audioSource ? audioSource.clip : null;
    public float EffectiveVolume => basePlaybackVolume * currentClipGain * occlusionMultiplier;

    private void Reset() => audioSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource) audioSource.volume = basePlaybackVolume;
        if (musicState) musicState.StateChanged += HandleMusicStateChanged;
    }

    private void Start()
    {
        ApplyMusicState(musicState ? musicState.State : AdaptiveMusicState.Regular);
        if (playlistRoutine == null) playlistRoutine = StartCoroutine(PlayPlaylist());
    }

    private void Update()
    {
        if (!audioSource) return;
        float pitchSpeed = Mathf.Abs(stressPitch - regularPitch) / Mathf.Max(0.05f, pitchTransitionSeconds);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, pitchSpeed * Time.unscaledDeltaTime);
        audioSource.volume = Mathf.SmoothDamp(
            audioSource.volume,
            EffectiveVolume,
            ref volumeVelocity,
            volumeTransitionSeconds,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    public void SetOcclusionMultiplier(float multiplier)
    {
        occlusionMultiplier = Mathf.Clamp01(multiplier);
    }

    private float GetClipGain(int index)
    {
        if (clipVolumeMultipliers == null || index < 0 || index >= clipVolumeMultipliers.Length)
            return 1f;
        return Mathf.Max(0f, clipVolumeMultipliers[index]);
    }

    private void HandleMusicStateChanged(AdaptiveMusicState next) => ApplyMusicState(next);

    private void ApplyMusicState(AdaptiveMusicState next)
    {
        dangerActive = next == AdaptiveMusicState.Stress;
        targetPitch = dangerActive ? stressPitch : regularPitch;
        if (dangerActive) remainingPause = Mathf.Min(remainingPause, stressPauseSeconds);
    }

    private IEnumerator PlayPlaylist()
    {
        while (enabled)
        {
            if (!audioSource || playlist == null || playlist.Length == 0)
            {
                yield return null;
                continue;
            }

            while (remainingPause > 0f)
            {
                remainingPause -= Time.unscaledDeltaTime;
                yield return null;
            }

            int currentIndex = nextIndex % playlist.Length;
            AudioClip next = playlist[currentIndex];
            nextIndex = (nextIndex + 1) % playlist.Length;
            if (!next)
            {
                yield return null;
                continue;
            }

            currentClipGain = GetClipGain(currentIndex);
            audioSource.clip = next;
            audioSource.Play();
            PlayedCount++;
            while (audioSource.isPlaying) yield return null;
            remainingPause = dangerActive ? stressPauseSeconds : regularPauseSeconds;
        }
        playlistRoutine = null;
    }

    private void OnDisable()
    {
        if (musicState) musicState.StateChanged -= HandleMusicStateChanged;
        if (playlistRoutine != null) StopCoroutine(playlistRoutine);
        playlistRoutine = null;
        occlusionMultiplier = 1f;
        currentClipGain = 1f;
        volumeVelocity = 0f;
        if (audioSource)
        {
            audioSource.Stop();
            audioSource.volume = basePlaybackVolume;
        }
    }
}
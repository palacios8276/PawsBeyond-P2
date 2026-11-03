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
    [Header("Transmisión normal")]
    [SerializeField, Range(0.5f, 2f)] private float regularPitch = 1f;
    [SerializeField, Min(0f)] private float regularPauseSeconds = 1.5f;
    [Header("Transmisión en peligro")]
    [SerializeField, Range(0.5f, 2f)] private float stressPitch = 1.15f;
    [SerializeField, Min(0f)] private float stressPauseSeconds = 0f;
    [SerializeField, Min(0.05f)] private float pitchTransitionSeconds = 0.45f;

    private Coroutine playlistRoutine;
    private float targetPitch = 1f;
    private float remainingPause;
    private int nextIndex;
    private bool dangerActive;

    public int PlayedCount { get; private set; }
    public int NextIndex => nextIndex;
    public bool DangerActive => dangerActive;
    public AudioClip CurrentClip => audioSource ? audioSource.clip : null;

    private void Reset() => audioSource = GetComponent<AudioSource>();

    private void OnEnable()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
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
        float speed = Mathf.Abs(stressPitch - regularPitch) / Mathf.Max(0.05f, pitchTransitionSeconds);
        audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, targetPitch, speed * Time.unscaledDeltaTime);
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

            AudioClip next = playlist[nextIndex % playlist.Length];
            nextIndex = (nextIndex + 1) % playlist.Length;
            if (!next)
            {
                yield return null;
                continue;
            }

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
        if (audioSource) audioSource.Stop();
    }
}

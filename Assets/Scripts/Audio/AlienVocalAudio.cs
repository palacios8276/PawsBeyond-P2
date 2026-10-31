using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class AlienVocalAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float presenceVolume = 0.22f;
    [SerializeField, Range(1f, 2f)] private float reactionVolumeMultiplier = 1.25f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);
    [SerializeField, Min(0.05f)] private float retreatFadeSeconds = 0.35f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlayPresence()
    {
        if (!audioSource || !audioSource.clip) return;
        CancelFade();
        audioSource.volume = presenceVolume;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.Stop();
        audioSource.Play();
    }

    public void PlayReaction()
    {
        if (!audioSource || !audioSource.clip) return;
        CancelFade();
        audioSource.volume = Mathf.Clamp01(presenceVolume * reactionVolumeMultiplier);
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.Stop();
        audioSource.Play();
    }

    public void BeginRetreat()
    {
        if (!audioSource || !audioSource.isPlaying) return;
        CancelFade();
        fadeRoutine = StartCoroutine(FadeAndStop());
    }

    private IEnumerator FadeAndStop()
    {
        float startVolume = audioSource.volume;
        for (float elapsed = 0f; elapsed < retreatFadeSeconds; elapsed += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / retreatFadeSeconds);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = presenceVolume;
        fadeRoutine = null;
    }

    private void CancelFade()
    {
        if (fadeRoutine == null) return;
        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private void OnDisable()
    {
        CancelFade();
        if (audioSource) audioSource.Stop();
    }
}

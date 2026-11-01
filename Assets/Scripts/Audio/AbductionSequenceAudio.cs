using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class AbductionSequenceAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Min(0.05f)] private float fadeOutSeconds = 0.55f;

    private Coroutine fadeRoutine;
    private float baseVolume;

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        baseVolume = audioSource.volume;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void PlaySequence()
    {
        CancelFade();
        if (!audioSource || !audioSource.clip) return;
        audioSource.volume = baseVolume;
        audioSource.Stop();
        audioSource.Play();
    }

    public void StopSequence()
    {
        if (!audioSource || !audioSource.isPlaying || fadeRoutine != null) return;
        fadeRoutine = StartCoroutine(FadeAndStop());
    }

    private IEnumerator FadeAndStop()
    {
        float startVolume = audioSource.volume;
        for (float elapsed = 0f; elapsed < fadeOutSeconds; elapsed += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutSeconds);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = baseVolume;
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

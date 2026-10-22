using UnityEngine;

// Preparado para un evento narrativo futuro; no agrega teclas ni reproducción automática.
public sealed class DogBarkAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] variants;
    private int nextVariant;

    public void PlayBark()
    {
        if (!isActiveAndEnabled || !audioSource || audioSource.isPlaying
            || variants == null || variants.Length == 0) return;
        var clip = variants[nextVariant % variants.Length];
        if (!clip) return;
        nextVariant++;
        audioSource.clip = clip;
        audioSource.Play();
    }
}

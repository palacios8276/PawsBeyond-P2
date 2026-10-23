using UnityEngine;

public sealed class DogBarkAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] variants;
    private int nextVariant;

    public void PlayBark() => TryPlayBark();

    public bool TryPlayBark()
    {
        if (!isActiveAndEnabled || !audioSource || !audioSource.isActiveAndEnabled
            || variants == null || variants.Length == 0) return false;
        var clip = variants[nextVariant % variants.Length];
        if (!clip) return false;
        nextVariant++;
        // Un solo emisor: otra pulsación válida reemplaza el clip, sin acumular voces.
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        return true;
    }
}

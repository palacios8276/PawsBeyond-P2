using UnityEngine;

public sealed class DogBarkAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] variants;
    [SerializeField] private AudioClip reinforcedClip;
    [SerializeField] private WorldComicFeedback barkComic;
    [SerializeField, Range(1f, 2f)] private float reinforcedVolumeMultiplier = 1.15f;
    [SerializeField, Range(0.5f, 1.5f)] private float reinforcedPitchMultiplier = 0.9f;
    private float baseVolume, basePitch;
    private bool levelsCaptured;
    private int nextVariant;

    public void PlayBark() => TryPlayBark();

    public bool TryPlayBark() => TryPlayBark(false);

    public bool TryPlayBark(bool reinforced)
    {
        if (!isActiveAndEnabled || !audioSource || !audioSource.isActiveAndEnabled
            || variants == null || variants.Length == 0) return false;
        var clip = reinforced ? reinforcedClip : variants[nextVariant % variants.Length];
        if (!clip) return false;
        if (!levelsCaptured)
        { baseVolume = audioSource.volume; basePitch = audioSource.pitch; levelsCaptured = true; }
        audioSource.volume = Mathf.Clamp01(baseVolume * (reinforced ? reinforcedVolumeMultiplier : 1f));
        audioSource.pitch = basePitch * (reinforced ? reinforcedPitchMultiplier : 1f);
        nextVariant++;
        // Un solo emisor: otra pulsación válida reemplaza el clip, sin acumular voces.
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
        if (barkComic) barkComic.ShowMessage(reinforced ? "WOOF! WOOF!" : "WOOF!");
        return true;
    }
    private void OnDisable()
    {
        if (levelsCaptured && audioSource)
        { audioSource.volume = baseVolume; audioSource.pitch = basePitch; }
        levelsCaptured = false;
    }
}

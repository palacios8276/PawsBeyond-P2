using System.Collections;
using UnityEngine;

public sealed class PlayerGameplayLock : MonoBehaviour
{
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private PlayerBark bark;
    [SerializeField] private MonoBehaviour[] additionalGameplayControls;
    [SerializeField] private AudioSource[] normalGameplaySources;
    [SerializeField, Min(0.05f)] private float audioFadeSeconds = 0.65f;

    public bool IsLocked { get; private set; }
    public int LockCount { get; private set; }

    public void LockForClosing()
    {
        if (IsLocked) return;
        IsLocked = true;
        LockCount++;
        if (movement) movement.enabled = false;
        if (interaction) interaction.enabled = false;
        if (bark) bark.enabled = false;
        if (additionalGameplayControls != null)
            foreach (MonoBehaviour control in additionalGameplayControls)
                if (control) control.enabled = false;
        StartCoroutine(FadeNormalGameplayAudio());
    }

    private IEnumerator FadeNormalGameplayAudio()
    {
        if (normalGameplaySources == null || normalGameplaySources.Length == 0) yield break;
        var startingVolumes = new float[normalGameplaySources.Length];
        for (int i = 0; i < normalGameplaySources.Length; i++)
            if (normalGameplaySources[i]) startingVolumes[i] = normalGameplaySources[i].volume;
        for (float elapsed = 0f; elapsed < audioFadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float weight = 1f - Mathf.SmoothStep(0f, 1f, elapsed / audioFadeSeconds);
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < normalGameplaySources.Length; i++)
                if (normalGameplaySources[i]) normalGameplaySources[i].volume = startingVolumes[i] * weight;
        }
        foreach (AudioSource source in normalGameplaySources)
        {
            if (!source) continue;
            source.volume = 0f;
            source.Stop();
        }
    }
}

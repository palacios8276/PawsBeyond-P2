using UnityEngine;

public sealed class TelevisionScreenGlow : MonoBehaviour
{
    [SerializeField] private Renderer screen;
    [SerializeField] private AudioSource televisionAudio;
    [SerializeField] private Light screenLight;
    [SerializeField] private Color screenColor = new Color(0.3f, 0.55f, 0.7f);
    [SerializeField, Min(0f)] private float emission = 1.2f;
    [SerializeField, Min(0f)] private float lightIntensity = 0.18f;
    [SerializeField, Min(0.1f)] private float variationSeconds = 3f;
    private MaterialPropertyBlock block, original;
    private void OnEnable()
    {
        block = new MaterialPropertyBlock(); original = new MaterialPropertyBlock();
        screen.GetPropertyBlock(original);
    }
    private void Update()
    {
        float variation = 0.9f + 0.1f * Mathf.Sin(Time.time * Mathf.PI * 2f / variationSeconds);
        float activity = televisionAudio && televisionAudio.isPlaying ? 1f : 0.65f;
        screen.GetPropertyBlock(block);
        block.SetColor("_BaseColor", screenColor * variation);
        block.SetColor("_EmissionColor", screenColor * emission * variation * activity);
        screen.SetPropertyBlock(block);
        screenLight.intensity = lightIntensity * variation * activity;
    }
    private void OnDisable()
    {
        if (screen && original != null) screen.SetPropertyBlock(original);
        if (screenLight) screenLight.intensity = 0;
    }
}

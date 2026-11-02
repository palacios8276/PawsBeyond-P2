using UnityEngine;

public sealed class TelevisionScreenGlow : MonoBehaviour
{
    [SerializeField] private Renderer screen;
    [SerializeField] private AudioSource televisionAudio;
    [SerializeField] private Light screenLight;
    [SerializeField] private Color screenColor = new Color(0.62f, 0.65f, 0.68f);
    [SerializeField, Min(0f)] private float emission = 0.85f;
    [SerializeField, Min(0f)] private float lightIntensity = 0.14f;
    [SerializeField, Min(0.1f)] private float variationSeconds = 3f;
    [Header("Estática analógica")]
    [SerializeField, Range(16, 128)] private int textureSize = 64;
    [SerializeField, Range(4f, 20f)] private float staticFramesPerSecond = 10f;
    [SerializeField, Range(0.05f, 0.45f)] private float noiseContrast = 0.26f;
    private MaterialPropertyBlock block, original;
    private Texture2D staticTexture;
    private Color32[] pixels;
    private readonly System.Random random = new System.Random();
    private float nextStaticFrame;

    private void OnEnable()
    {
        block = new MaterialPropertyBlock(); original = new MaterialPropertyBlock();
        if (screen) screen.GetPropertyBlock(original);
        CreateStaticTexture();
    }

    private void Update()
    {
        if (!screen || !screenLight) return;
        if (Time.unscaledTime >= nextStaticFrame)
        {
            nextStaticFrame = Time.unscaledTime + 1f / staticFramesPerSecond;
            RefreshStatic();
        }
        float variation = 0.96f + 0.04f * Mathf.Sin(Time.time * Mathf.PI * 2f / variationSeconds);
        screen.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", staticTexture);
        block.SetTexture("_EmissionMap", staticTexture);
        block.SetColor("_BaseColor", screenColor * variation);
        block.SetColor("_EmissionColor", screenColor * emission * variation);
        screen.SetPropertyBlock(block);
        screenLight.intensity = lightIntensity * variation;
    }

    private void CreateStaticTexture()
    {
        int size = Mathf.Clamp(textureSize, 16, 128);
        staticTexture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "TV_Static_Runtime",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.DontSave
        };
        pixels = new Color32[size * size];
        RefreshStatic();
    }

    private void RefreshStatic()
    {
        if (!staticTexture || pixels == null) return;
        int spread = Mathf.RoundToInt(255f * noiseContrast);
        for (int i = 0; i < pixels.Length; i++)
        {
            byte value = (byte)Mathf.Clamp(142 + random.Next(-spread, spread + 1), 24, 235);
            pixels[i] = new Color32(value, value, value, 255);
        }
        staticTexture.SetPixels32(pixels);
        staticTexture.Apply(false, false);
    }

    private void OnDisable()
    {
        if (screen && original != null) screen.SetPropertyBlock(original);
        if (screenLight) screenLight.intensity = 0;
        if (staticTexture) Destroy(staticTexture);
        staticTexture = null;
        pixels = null;
    }
}

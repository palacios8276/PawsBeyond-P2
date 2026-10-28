using UnityEngine;

public sealed class AlienHideCue : MonoBehaviour
{
    [SerializeField] private Renderer furniture;
    [SerializeField] private Light cueLight;
    [SerializeField] private Color cueColor = new Color(0.3f, 0.9f, 0.7f);
    [SerializeField, Min(0.1f)] private float pulseSpeed = 1.3f;
    [SerializeField, Range(0f, 1f)] private float maximumBlend = 0.22f;
    private MaterialPropertyBlock original, block;
    private Color baseColor;
    private void OnEnable()
    {
        if (!furniture) return;
        original = new MaterialPropertyBlock(); block = new MaterialPropertyBlock();
        furniture.GetPropertyBlock(original); furniture.GetPropertyBlock(block);
        baseColor = furniture.sharedMaterial.GetColor("_BaseColor");
    }
    private void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        if (furniture && block != null) { block.SetColor("_BaseColor", Color.Lerp(baseColor, cueColor, pulse * maximumBlend)); furniture.SetPropertyBlock(block); }
        if (cueLight) cueLight.intensity = Mathf.Lerp(0.1f, 0.7f, pulse);
    }
    private void OnDisable()
    {
        if (furniture && original != null) furniture.SetPropertyBlock(original);
        if (cueLight) cueLight.intensity = 0;
    }
}

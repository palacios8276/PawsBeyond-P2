using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private Color outlineColor = new Color(1f, 0.32f, 0.02f, 0.82f);
    [SerializeField, Range(1.005f, 1.15f)] private float outlineScale = 1.045f;
    [SerializeField, Range(0f, 0.04f)] private float pulseScale = 0.012f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.2f;

    private readonly List<Renderer> outlines = new List<Renderer>();
    private MaterialPropertyBlock block;
    private static Material sharedOutlineMaterial;
    public bool IsHighlighted { get; private set; }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        BuildOutlines();
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        IsHighlighted = highlighted;
        foreach (Renderer outline in outlines)
            if (outline) outline.enabled = highlighted;
    }

    private void Update()
    {
        if (!IsHighlighted) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
        float scale = outlineScale + pulseScale * pulse;
        Color color = outlineColor;
        color.a *= Mathf.Lerp(0.72f, 1f, pulse);
        block.SetColor("_BaseColor", color);
        foreach (Renderer outline in outlines)
        {
            if (!outline) continue;
            outline.transform.localScale = Vector3.one * scale;
            outline.SetPropertyBlock(block);
        }
    }

    private void BuildOutlines()
    {
        Material material = GetOutlineMaterial();
        if (!material) return;
        foreach (MeshRenderer source in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (source.gameObject.name.StartsWith("InteractionOutline_")) continue;
            MeshFilter filter = source.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;
            var outlineObject = new GameObject("InteractionOutline_" + source.gameObject.name);
            outlineObject.hideFlags = HideFlags.DontSave;
            outlineObject.transform.SetParent(source.transform, false);
            outlineObject.transform.localScale = Vector3.one * outlineScale;
            var outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = filter.sharedMesh;
            var outline = outlineObject.AddComponent<MeshRenderer>();
            var materials = new Material[Mathf.Max(1, filter.sharedMesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            outline.sharedMaterials = materials;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.lightProbeUsage = LightProbeUsage.Off;
            outlines.Add(outline);
        }
    }

    private static Material GetOutlineMaterial()
    {
        if (sharedOutlineMaterial) return sharedOutlineMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) return null;
        sharedOutlineMaterial = new Material(shader)
        {
            name = "InteractionOutline_Runtime",
            hideFlags = HideFlags.DontSave,
            renderQueue = (int)RenderQueue.Transparent + 1
        };
        sharedOutlineMaterial.SetFloat("_Surface", 1f);
        sharedOutlineMaterial.SetFloat("_Blend", 0f);
        sharedOutlineMaterial.SetFloat("_Cull", (float)CullMode.Front);
        sharedOutlineMaterial.SetFloat("_ZWrite", 0f);
        sharedOutlineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        sharedOutlineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        sharedOutlineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return sharedOutlineMaterial;
    }

    private void OnDisable() => SetHighlighted(false);
}

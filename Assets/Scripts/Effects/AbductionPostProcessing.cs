using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public sealed class AbductionPostProcessing : MonoBehaviour
{
    [Serializable]
    private struct VisualStyle
    {
        [Min(0f)] public float bloom;
        [Range(-100f, 100f)] public float contrast;
        [Range(0f, 0.4f)] public float vignette;
        public VisualStyle(float bloom, float contrast, float vignette)
        { this.bloom = bloom; this.contrast = contrast; this.vignette = vignette; }
    }

    [SerializeField] private Volume globalVolume;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField, Min(0.1f)] private float transitionSeconds = 0.65f;
    [SerializeField] private VisualStyle stable = new VisualStyle(0.22f, 3f, 0.16f);
    [SerializeField] private VisualStyle affected = new VisualStyle(0.27f, 4f, 0.17f);
    [SerializeField] private VisualStyle intense = new VisualStyle(0.34f, 6f, 0.19f);
    [SerializeField] private VisualStyle uncontrolled = new VisualStyle(0.42f, 8f, 0.21f);
    private Bloom bloom;
    private ColorAdjustments colors;
    private Vignette vignette;
    private VisualStyle original;
    private Coroutine transition;
    public AbductionBeamState VisualState { get; private set; }

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        // Volume crea una instancia en memoria: los cambios de estado no escriben el asset.
        if (!globalVolume || !globalVolume.sharedProfile || !beam)
        { Debug.LogError("Postprocesado: faltan Volume, perfil o rayo.", this); enabled = false; return; }
        var profile = globalVolume.profile;
        if (!profile.TryGet(out bloom) || !profile.TryGet(out colors) || !profile.TryGet(out vignette))
        { Debug.LogError("Postprocesado: faltan Bloom, ColorAdjustments o Vignette.", this); enabled = false; return; }
        original = CurrentStyle();
        VisualState = beam.IsBeamActive ? beam.CurrentState : AbductionBeamState.Stable;
        Apply(StyleFor(VisualState));
    }

    // Recibe el evento existente del rayo; no calcula ni modifica interferencia.
    public void OnBeamStateChanged(AbductionBeamState state)
    {
        if (!isActiveAndEnabled || bloom == null) return;
        state = beam.IsBeamActive ? state : AbductionBeamState.Stable;
        if (VisualState == state) return;
        VisualState = state;
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(BlendTo(StyleFor(state)));
    }

    private VisualStyle StyleFor(AbductionBeamState state) => state == AbductionBeamState.Uncontrolled ? uncontrolled
        : state == AbductionBeamState.IntenseInterference ? intense
        : state == AbductionBeamState.Affected ? affected : stable;

    private VisualStyle CurrentStyle() => new VisualStyle(bloom.intensity.value, colors.contrast.value, vignette.intensity.value);
    private void Apply(VisualStyle style)
    {
        bloom.intensity.value = style.bloom;
        colors.contrast.value = style.contrast;
        vignette.intensity.value = style.vignette;
    }

    private IEnumerator BlendTo(VisualStyle target)
    {
        VisualStyle from = CurrentStyle();
        for (float elapsed = 0; elapsed < transitionSeconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / transitionSeconds);
            Apply(new VisualStyle(Mathf.Lerp(from.bloom, target.bloom, t),
                Mathf.Lerp(from.contrast, target.contrast, t), Mathf.Lerp(from.vignette, target.vignette, t)));
            yield return null;
        }
        Apply(target);
        transition = null;
    }

    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        if (bloom != null && colors != null && vignette != null) Apply(original);
    }
}

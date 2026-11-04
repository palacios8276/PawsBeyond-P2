using System;
using System.Collections;
using UnityEngine;

public enum AdaptiveMusicState { Regular, Stress, FadingOut }

// Autoridad única de música: observa eventos narrativos y no modifica el gameplay.
[DisallowMultipleComponent]
public sealed class AdaptiveMusicController : MonoBehaviour
{
    [SerializeField] private AudioSource regularSource;
    [SerializeField] private AudioSource stressSource;
    [SerializeField, Min(0.1f)] private float crossfadeSeconds = 2f;
    [SerializeField, Range(0f, 1f)] private float regularLevel = 0.26f;
    [SerializeField, Range(0f, 1f)] private float stressLevel = 0.3f;
    [SerializeField] private AdaptiveMusicState state = AdaptiveMusicState.Regular;

    private Coroutine transition;
    private bool threatActive;
    private bool closingActive;

    public AdaptiveMusicState State => state;
    public float CrossfadeSeconds => crossfadeSeconds;
    public bool ThreatActive => threatActive;
    public int TransitionCount { get; private set; }
    public event Action<AdaptiveMusicState> StateChanged;

    private void Awake()
    {
        Prepare(regularSource);
        Prepare(stressSource);
        if (regularSource) regularSource.volume = 0f;
        if (stressSource) stressSource.volume = 0f;
    }

    private void Start() => BeginTransition(AdaptiveMusicState.Regular);

    public void OnThreatStarted()
    {
        threatActive = true;
        BeginTransition(AdaptiveMusicState.Stress);
    }

    public void OnThreatEnded()
    {
        threatActive = false;
        if (!closingActive) BeginTransition(AdaptiveMusicState.Regular);
    }

    // Conserva las conexiones serializadas de las fases anteriores.
    public void OnAbductionStarted() => OnThreatStarted();
    public void OnOperatorsRetreated() => OnThreatEnded();

    public void RequestRegular()
    {
        if (!threatActive && !closingActive) BeginTransition(AdaptiveMusicState.Regular);
    }

    public void RequestStress() => BeginTransition(AdaptiveMusicState.Stress);

    public void OnClosingStarted()
    {
        closingActive = true;
        BeginTransition(AdaptiveMusicState.Stress);
    }

    public void OnContinueShown()
    {
        if (state == AdaptiveMusicState.FadingOut) return;
        SetState(AdaptiveMusicState.FadingOut);
        RestartTransition(FadeOut());
    }

    private static void Prepare(AudioSource source)
    {
        if (!source) return;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void BeginTransition(AdaptiveMusicState next)
    {
        if (next == AdaptiveMusicState.FadingOut) return;
        AudioSource target = next == AdaptiveMusicState.Stress ? stressSource : regularSource;
        if (!target) return;
        if (state == next && target.isPlaying && transition == null) return;
        SetState(next);
        TransitionCount++;
        RestartTransition(Crossfade(next));
    }

    private void SetState(AdaptiveMusicState next)
    {
        if (state == next) return;
        state = next;
        StateChanged?.Invoke(state);
    }

    private void RestartTransition(IEnumerator routine)
    {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(routine);
    }

    private IEnumerator Crossfade(AdaptiveMusicState next)
    {
        AudioSource target = next == AdaptiveMusicState.Stress ? stressSource : regularSource;
        AudioSource other = next == AdaptiveMusicState.Stress ? regularSource : stressSource;
        float targetLevel = next == AdaptiveMusicState.Stress ? stressLevel : regularLevel;
        if (!target.isPlaying) target.Play();
        float targetStart = target.volume;
        float otherStart = other ? other.volume : 0f;
        for (float elapsed = 0f; elapsed < crossfadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / crossfadeSeconds);
            target.volume = Mathf.Lerp(targetStart, targetLevel, t);
            if (other) other.volume = Mathf.Lerp(otherStart, 0f, t);
            yield return null;
        }
        target.volume = targetLevel;
        if (other)
        {
            other.volume = 0f;
            other.Stop();
        }
        transition = null;
    }

    private IEnumerator FadeOut()
    {
        float regularStart = regularSource ? regularSource.volume : 0f;
        float stressStart = stressSource ? stressSource.volume : 0f;
        for (float elapsed = 0f; elapsed < crossfadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / crossfadeSeconds);
            if (regularSource) regularSource.volume = Mathf.Lerp(regularStart, 0f, t);
            if (stressSource) stressSource.volume = Mathf.Lerp(stressStart, 0f, t);
            yield return null;
        }
        StopSource(regularSource);
        StopSource(stressSource);
        transition = null;
    }

    private static void StopSource(AudioSource source)
    {
        if (!source) return;
        source.volume = 0f;
        source.Stop();
    }

    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        StopSource(regularSource);
        StopSource(stressSource);
    }
}

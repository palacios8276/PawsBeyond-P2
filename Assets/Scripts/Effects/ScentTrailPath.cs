using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ScentTrailPath : MonoBehaviour
{
    [SerializeField] private Transform observer;
    [SerializeField] private Transform[] routePoints;
    [SerializeField] private Color scentColor = new Color(1f, 0.62f, 0.18f, 0.22f);
    [SerializeField, Range(0f, 2f)] private float intensity = 0.65f;
    [SerializeField, Min(0.5f)] private float visibleRange = 5.5f;
    [SerializeField, Min(0.25f)] private float sampleSpacing = 1.1f;
    [SerializeField, Min(0.1f)] private float particleDuration = 1.8f;
    [SerializeField, Min(0.02f)] private float particleSize = 0.16f;
    [SerializeField, Min(0.05f)] private float refreshSeconds = 0.45f;
    [SerializeField, Min(1)] private int maximumParticles = 80;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.12f, 0f);

    private readonly List<Vector3> samples = new List<Vector3>();
    private ParticleSystem particles;
    private Material runtimeMaterial;
    private float nextRefresh;
    public int VisibleSampleCount { get; private set; }

    private void Awake()
    {
        var emitter = new GameObject("ScentParticles_Runtime");
        emitter.transform.SetParent(transform, false);
        particles = emitter.AddComponent<ParticleSystem>();
        ConfigureParticles();
        RebuildSamples();
    }

    private void OnEnable()
    {
        nextRefresh = 0f;
        if (samples.Count == 0) RebuildSamples();
    }

    private void Update()
    {
        if (!observer || Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshSeconds;
        RefreshVisibleTrail();
    }

    public void RebuildSamples()
    {
        samples.Clear();
        if (routePoints == null || routePoints.Length == 0) return;
        Transform previous = null;
        foreach (Transform point in routePoints)
        {
            if (!point) continue;
            if (!previous) samples.Add(point.position + worldOffset);
            else
            {
                float distance = Vector3.Distance(previous.position, point.position);
                int steps = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));
                for (int i = 1; i <= steps; i++)
                    samples.Add(Vector3.Lerp(previous.position, point.position, i / (float)steps) + worldOffset);
            }
            previous = point;
        }
    }

    private void RefreshVisibleTrail()
    {
        particles.Clear(true);
        VisibleSampleCount = 0;
        float rangeSqr = visibleRange * visibleRange;
        foreach (Vector3 sample in samples)
        {
            if (VisibleSampleCount >= maximumParticles) break;
            float distanceSqr = (sample - observer.position).sqrMagnitude;
            if (distanceSqr > rangeSqr) continue;
            float proximity = 1f - Mathf.Sqrt(distanceSqr) / visibleRange;
            Color color = scentColor;
            color.a *= intensity * Mathf.SmoothStep(0.2f, 1f, proximity);
            var emit = new ParticleSystem.EmitParams
            {
                position = sample,
                velocity = Vector3.up * (0.025f + 0.035f * proximity),
                startColor = color,
                startLifetime = particleDuration,
                startSize = particleSize * Mathf.Lerp(0.75f, 1.25f, proximity)
            };
            particles.Emit(emit, 1);
            VisibleSampleCount++;
        }
    }

    private void ConfigureParticles()
    {
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximumParticles;
        main.startSpeed = 0f;
        main.startLifetime = particleDuration;
        main.startSize = particleSize;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!shader) shader = Shader.Find("Particles/Standard Unlit");
        if (!shader) return;
        runtimeMaterial = new Material(shader) { name = name + "_ScentRuntime" };
        runtimeMaterial.SetColor("_BaseColor", Color.white);
        renderer.sharedMaterial = runtimeMaterial;
    }

    private void OnDisable()
    {
        if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        VisibleSampleCount = 0;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial) Destroy(runtimeMaterial);
    }
}

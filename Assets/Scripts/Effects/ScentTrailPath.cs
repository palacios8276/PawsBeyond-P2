using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public enum ScentTrailKind
{
    Human,
    Alien
}

public sealed class ScentTrailPath : MonoBehaviour
{
    [Header("Activación y fuentes")]
    [SerializeField] private bool scentEnabled = true;
    [SerializeField] private ScentTrailKind scentKind;
    [SerializeField] private Transform observer;
    [SerializeField] private Transform[] emitters;

    [Header("Visibilidad")]
    [SerializeField, Min(0.5f)] private float visibleRange = 5f;
    [SerializeField, Min(0.1f)] private float sampleSpacing = 0.65f;
    [SerializeField, Min(0.02f)] private float particleSize = 0.09f;
    [SerializeField, Range(0f, 2f)] private float intensity = 0.55f;
    [SerializeField, Range(0.5f, 2f)] private float colorBrightness = 1.12f;
    [SerializeField, Range(0f, 0.35f)] private float softHaloOpacity = 0.12f;
    [SerializeField, Range(1f, 2.5f)] private float softHaloScale = 1.65f;
    [Header("Legibilidad contextual por distancia real")]
    [SerializeField, Min(0f)] private float contextualDistanceThreshold = 6f;
    [SerializeField, Min(0.1f)] private float contextualMaximumDistance = 14f;
    [SerializeField, Range(1f, 1.75f)] private float distantBrightnessMultiplier = 1.28f;
    [SerializeField, Min(0.1f)] private float brightnessTransitionSpeed = 2.5f;
    [SerializeField, Min(0.05f)] private float refreshSeconds = 0.15f;
    [SerializeField, Min(1)] private int maximumMarks = 48;
    [SerializeField, Min(0.5f)] private float teleportThreshold = 3f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.12f, 0f);

    [Header("Identidad humana")]
    [SerializeField] private Color humanColor = new Color(1f, 0.55f, 0.18f, 0.42f);
    [SerializeField, Min(0.1f)] private float humanDuration = 8f;

    [Header("Identidad alienígena")]
    [SerializeField] private Color alienColor = new Color(0.2f, 0.82f, 0.88f, 0.22f);
    [SerializeField, Min(0.1f)] private float alienDuration = 3.5f;

    private sealed class EmitterTracker
    {
        public Transform Source;
        public Vector3 PreviousPosition;
        public float DistanceSinceMark;
        public bool WasEligible;
    }

    private struct ScentMark
    {
        public Vector3 Position;
        public float CreatedAt;
        public float VisualSeed;
    }

    private readonly List<EmitterTracker> trackers = new List<EmitterTracker>();
    private readonly List<ScentMark> marks = new List<ScentMark>();
    private ParticleSystem particles;
    private Material runtimeMaterial;
    private Texture2D softParticleTexture;
    private float nextRefresh;
    private float contextualBrightness = 1f;

    public int RecordedMarkCount => marks.Count;
    public int VisibleSampleCount { get; private set; }
    public bool IsScentEnabled => scentEnabled;
    public ScentTrailKind Kind => scentKind;
    public float ContextualBrightness => contextualBrightness;

    private float ActiveDuration => scentKind == ScentTrailKind.Human ? humanDuration : alienDuration;
    private Color ActiveColor => scentKind == ScentTrailKind.Human ? humanColor : alienColor;

    private void Awake()
    {
        CreateParticleRenderer();
        RebuildEmitters();
    }

    private void OnEnable()
    {
        nextRefresh = 0f;
        RebuildEmitters();
        ClearTrail();
    }

    private void Update()
    {
        if (!scentEnabled)
        {
            if (marks.Count > 0 || VisibleSampleCount > 0) ClearTrail();
            return;
        }

        TrackRealMovement();
        RemoveExpiredMarks();
        UpdateContextualBrightness();

        if (!observer || Time.time < nextRefresh) return;
        nextRefresh = Time.time + refreshSeconds;
        RefreshVisibleTrail();
    }

    public void SetScentEnabled(bool enabled)
    {
        scentEnabled = enabled;
        if (!enabled) ClearTrail();
    }

    public void RebuildEmitters()
    {
        trackers.Clear();
        if (emitters == null) return;

        foreach (Transform source in emitters)
        {
            if (!source) continue;
            trackers.Add(new EmitterTracker
            {
                Source = source,
                PreviousPosition = source.position
            });
        }
    }

    public void ClearTrail()
    {
        marks.Clear();
        VisibleSampleCount = 0;
        foreach (EmitterTracker tracker in trackers)
        {
            if (!tracker.Source) continue;
            tracker.PreviousPosition = tracker.Source.position;
            tracker.DistanceSinceMark = 0f;
            tracker.WasEligible = false;
        }
        if (particles) particles.Clear(true);
    }

    private void TrackRealMovement()
    {
        foreach (EmitterTracker tracker in trackers)
        {
            if (!tracker.Source) continue;

            Vector3 current = tracker.Source.position;
            bool eligible = IsRelevantMovement(tracker.Source);
            if (!eligible)
            {
                tracker.PreviousPosition = current;
                tracker.DistanceSinceMark = 0f;
                tracker.WasEligible = false;
                continue;
            }

            if (!tracker.WasEligible)
            {
                tracker.PreviousPosition = current;
                tracker.DistanceSinceMark = 0f;
                tracker.WasEligible = true;
                continue;
            }

            float segmentDistance = Vector3.Distance(tracker.PreviousPosition, current);
            if (segmentDistance > teleportThreshold)
            {
                tracker.PreviousPosition = current;
                tracker.DistanceSinceMark = 0f;
                continue;
            }

            if (segmentDistance <= Mathf.Epsilon) continue;

            Vector3 segmentStart = tracker.PreviousPosition;
            float distanceLeft = segmentDistance;
            while (tracker.DistanceSinceMark + distanceLeft >= sampleSpacing)
            {
                float needed = sampleSpacing - tracker.DistanceSinceMark;
                float portion = needed / distanceLeft;
                Vector3 markPosition = Vector3.Lerp(segmentStart, current, portion);
                AddMark(markPosition + worldOffset);
                segmentStart = markPosition;
                distanceLeft -= needed;
                tracker.DistanceSinceMark = 0f;
            }

            tracker.DistanceSinceMark += distanceLeft;
            tracker.PreviousPosition = current;
        }
    }

    private bool IsRelevantMovement(Transform source)
    {
        if (!source.gameObject.activeInHierarchy) return false;

        OwnersJourney owners = source.GetComponentInParent<OwnersJourney>();
        if (owners) return owners.HasStarted && !owners.IsComplete;

        KitchenAlien alien = source.GetComponentInParent<KitchenAlien>();
        if (alien)
            return alien.State == KitchenAlienState.Running || alien.State == KitchenAlienState.Crawling;

        return true;
    }

    private void AddMark(Vector3 position)
    {
        if (marks.Count >= maximumMarks) marks.RemoveAt(0);
        marks.Add(new ScentMark
        {
            Position = position,
            CreatedAt = Time.time,
            VisualSeed = Random.Range(0f, 10f)
        });
    }

    private void RemoveExpiredMarks()
    {
        float oldestAllowed = Time.time - ActiveDuration;
        for (int i = marks.Count - 1; i >= 0; i--)
            if (marks[i].CreatedAt < oldestAllowed) marks.RemoveAt(i);
    }

    private void RefreshVisibleTrail()
    {
        if (!particles) return;
        particles.Clear(true);
        VisibleSampleCount = 0;

        float duration = ActiveDuration;
        float rangeSqr = visibleRange * visibleRange;
        Color baseColor = ActiveColor;

        foreach (ScentMark mark in marks)
        {
            float distanceSqr = (mark.Position - observer.position).sqrMagnitude;
            if (distanceSqr > rangeSqr) continue;

            float age01 = Mathf.Clamp01((Time.time - mark.CreatedAt) / duration);
            float proximity = 1f - Mathf.Sqrt(distanceSqr) / visibleRange;
            float fade = (1f - age01) * Mathf.SmoothStep(0.15f, 1f, proximity);
            Color color = baseColor;
            float effectiveBrightness = colorBrightness * contextualBrightness;
            color.r *= effectiveBrightness;
            color.g *= effectiveBrightness;
            color.b *= effectiveBrightness;
            color.a *= intensity * fade;
            if (color.a <= 0.01f) continue;

            float drift = age01 * particleSize * 2.2f;
            Vector3 position = mark.Position + Vector3.up * drift;
            position += new Vector3(Mathf.Sin(mark.VisualSeed + age01 * 4f), 0f,
                Mathf.Cos(mark.VisualSeed + age01 * 3f)) * particleSize * 0.22f;

            float scaleVariation = Mathf.Lerp(0.68f, 1.12f,
                Mathf.Sin(mark.VisualSeed * 1.73f) * 0.5f + 0.5f);
            color.a *= Mathf.Lerp(0.68f, 1f,
                Mathf.Cos(mark.VisualSeed * 2.11f) * 0.5f + 0.5f);

            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = Vector3.zero,
                startColor = color,
                startLifetime = refreshSeconds * 1.5f,
                startSize = particleSize * scaleVariation
            };
            particles.Emit(emit, 1);
            if (softHaloOpacity > 0f)
            {
                Color haloColor = color;
                haloColor.a *= softHaloOpacity;
                emit.startColor = haloColor;
                emit.startSize = particleSize * scaleVariation * softHaloScale;
                particles.Emit(emit, 1);
            }
            VisibleSampleCount++;
        }
    }

    private void UpdateContextualBrightness()
    {
        float nearestDistance = 0f;
        bool foundEmitter = false;
        if (observer)
        {
            foreach (EmitterTracker tracker in trackers)
            {
                if (!tracker.Source || !tracker.Source.gameObject.activeInHierarchy) continue;
                float distance = Vector3.Distance(observer.position, tracker.Source.position);
                if (!foundEmitter || distance < nearestDistance)
                {
                    nearestDistance = distance;
                    foundEmitter = true;
                }
            }
        }

        float target = 1f;
        if (foundEmitter && nearestDistance > contextualDistanceThreshold)
        {
            float maximum = Mathf.Max(contextualDistanceThreshold + 0.1f, contextualMaximumDistance);
            float distance01 = Mathf.InverseLerp(contextualDistanceThreshold, maximum, nearestDistance);
            target = Mathf.Lerp(1f, distantBrightnessMultiplier, distance01);
        }
        contextualBrightness = Mathf.MoveTowards(contextualBrightness, target,
            brightnessTransitionSpeed * Time.deltaTime * Mathf.Max(0.01f, distantBrightnessMultiplier - 1f));
    }

    private void CreateParticleRenderer()
    {
        var emitter = new GameObject("ScentParticles_Runtime");
        emitter.transform.SetParent(transform, false);
        particles = emitter.AddComponent<ParticleSystem>();

        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximumMarks * 2;
        main.startSpeed = 0f;

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

        softParticleTexture = CreateSoftParticleTexture();
        runtimeMaterial = new Material(shader) { name = name + "_ScentRuntime" };
        runtimeMaterial.SetColor("_BaseColor", Color.white);
        runtimeMaterial.SetTexture("_BaseMap", softParticleTexture);
        runtimeMaterial.SetTexture("_MainTex", softParticleTexture);
        runtimeMaterial.SetFloat("_Surface", 1f);
        runtimeMaterial.SetFloat("_Blend", 0f);
        runtimeMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        runtimeMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        runtimeMaterial.SetFloat("_ZWrite", 0f);
        runtimeMaterial.SetOverrideTag("RenderType", "Transparent");
        runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        runtimeMaterial.DisableKeyword("_ALPHATEST_ON");
        runtimeMaterial.renderQueue = (int)RenderQueue.Transparent;
        renderer.sharedMaterial = runtimeMaterial;
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ScentSoftCircle_Runtime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = (new Vector2(x, y) - center) / radius;
            float distance = point.magnitude;
            float angle = Mathf.Atan2(point.y, point.x);
            float unevenEdge = 0.78f + Mathf.Sin(angle * 3f + 0.7f) * 0.07f
                + Mathf.Sin(angle * 5f - 1.1f) * 0.045f;
            float alpha = Mathf.SmoothStep(unevenEdge, 0.08f, distance);
            float internalVariation = 0.82f + 0.18f * Mathf.Sin(point.x * 11f + point.y * 7f + 0.4f)
                * Mathf.Sin(point.y * 9f - point.x * 5f);
            alpha *= Mathf.Clamp01(internalVariation);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private void OnDisable()
    {
        if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        VisibleSampleCount = 0;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial) Destroy(runtimeMaterial);
        if (softParticleTexture) Destroy(softParticleTexture);
    }
}

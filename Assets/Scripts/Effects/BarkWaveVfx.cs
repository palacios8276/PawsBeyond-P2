using UnityEngine;

public sealed class BarkWaveVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystem wave;
    [SerializeField] private Transform mouth;
    [SerializeField, Min(0.05f)] private float duration = 0.4f;
    [SerializeField, Range(4, 32)] private int particlesPerWave = 12;
    [SerializeField, Min(0.01f)] private float particleSize = 0.08f;
    [SerializeField, Min(0f)] private float initialRadius = 0.06f;
    [SerializeField, Min(0f)] private float expansionSpeed = 0.8f;
    [SerializeField, Min(0f)] private float travelSpeed = 1.2f;
    [SerializeField] private Color color = new Color(0.3f, 0.9f, 1f, 0.9f);
    public int EmissionCount { get; private set; }
    public int LastEmissionFrame { get; private set; } = -1;

    private void Awake()
    {
        if (wave) { wave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); Configure(); }
    }

    private void Configure()
    {
        var main = wave.main;
        main.loop = false; main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = duration; main.startLifetime = duration;
        main.startSpeed = 0f; main.startSize = particleSize;
        main.maxParticles = particlesPerWave;
        var emission = wave.emission; emission.enabled = false;
        var shape = wave.shape; shape.enabled = false;
    }

    // Solo presentación: no conoce ni modifica el rayo, la IA o los puzzles.
    public void EmitWave()
    {
        if (!isActiveAndEnabled || !wave || !mouth) return;
        wave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        Configure();
        wave.Play(false);
        for (int i = 0; i < particlesPerWave; i++)
        {
            float angle = i * Mathf.PI * 2f / particlesPerWave;
            Vector3 radial = mouth.right * Mathf.Cos(angle) + mouth.up * Mathf.Sin(angle);
            var particle = new ParticleSystem.EmitParams
            {
                position = mouth.position + radial * initialRadius,
                velocity = mouth.forward * travelSpeed + radial * expansionSpeed,
                startLifetime = duration,
                startSize = particleSize,
                startColor = color
            };
            wave.Emit(particle, 1);
        }
        EmissionCount++;
        LastEmissionFrame = Time.frameCount;
    }

    private void OnDisable()
    {
        if (wave) wave.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

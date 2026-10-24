using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class TVBeamInterference : MonoBehaviour
{
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private Renderer beamVolume;
    [SerializeField, Min(0f)] private float nearDistance = 1f;
    [SerializeField, Min(0.1f)] private float farDistance = 5f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.5f;
    private AudioSource source;
    private float originalVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        originalVolume = source.volume;
        source.volume = 0f;
        source.Stop();
    }

    private void Update()
    {
        float proximity = 0f;
        if (beam && beam.IsBeamActive && beamVolume && beamVolume.enabled)
        {
            float distance = Vector3.Distance(transform.position, beamVolume.bounds.ClosestPoint(transform.position));
            proximity = 1f - Mathf.InverseLerp(nearDistance, Mathf.Max(nearDistance + 0.01f, farDistance), distance);
        }
        float target = originalVolume * proximity;
        source.volume = Mathf.MoveTowards(source.volume, target, originalVolume * Time.deltaTime / fadeSeconds);
        if (source.volume > 0.001f && source.clip && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Stop();
    }

    private void OnDisable()
    {
        if (!source) return;
        source.Stop();
        source.volume = originalVolume;
    }
}

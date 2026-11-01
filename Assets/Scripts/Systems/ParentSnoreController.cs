using UnityEngine;

public sealed class ParentSnoreController : MonoBehaviour
{
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private AudioSource[] sources;
    [SerializeField, Range(0f, 1f)] private float restingVolume = 0.12f;
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.8f;

    private bool stopping;

    private void Start()
    {
        if (!journey) journey = GetComponent<OwnersJourney>();
        if (sources == null) return;
        foreach (AudioSource source in sources)
        {
            if (!source || !source.clip) continue;
            source.loop = true;
            source.volume = restingVolume;
            if (!journey || !journey.HasStarted) source.Play();
        }
    }

    private void Update()
    {
        if (stopping || !journey || !journey.HasStarted) return;
        stopping = true;
    }

    private void LateUpdate()
    {
        if (!stopping || sources == null) return;
        float step = fadeSeconds <= 0f ? 1f : Time.deltaTime / fadeSeconds;
        bool audible = false;
        foreach (AudioSource source in sources)
        {
            if (!source) continue;
            source.volume = Mathf.MoveTowards(source.volume, 0f, step * restingVolume);
            audible |= source.volume > 0.001f;
        }
        if (audible) return;
        foreach (AudioSource source in sources)
            if (source) source.Stop();
        enabled = false;
    }
}

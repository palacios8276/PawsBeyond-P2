using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class TVAudioOcclusion : MonoBehaviour
{
    [SerializeField] private TVNewsController newsController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    [SerializeField] private Transform listenerTarget;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform sourceHierarchyRoot;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField, Min(0.05f)] private float checkInterval = 0.12f;
    [SerializeField, Range(0f, 1f)] private float occludedVolumeMultiplier = 0.48f;
    [SerializeField, Range(10f, 22000f)] private float occludedCutoffFrequency = 2800f;
    [SerializeField, Range(10f, 22000f)] private float clearCutoffFrequency = 22000f;
    [SerializeField, Min(0.1f)] private float transitionResponse = 8f;
    [SerializeField, Min(0f)] private float minimumObstacleExtent = 0.35f;

    private readonly RaycastHit[] hits = new RaycastHit[16];
    private float nextCheckTime;
    private float targetVolumeMultiplier = 1f;
    private float currentVolumeMultiplier = 1f;
    private float targetCutoffFrequency = 22000f;
    private float currentCutoffFrequency = 22000f;

    public bool IsOccluded { get; private set; }
    public float CurrentVolumeMultiplier => currentVolumeMultiplier;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        newsController = GetComponent<TVNewsController>();
        lowPassFilter = GetComponent<AudioLowPassFilter>();
    }

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!newsController) newsController = GetComponent<TVNewsController>();
        if (!lowPassFilter) lowPassFilter = GetComponent<AudioLowPassFilter>();
        ResolveListener();
    }

    private void OnEnable()
    {
        ResolveListener();
        currentVolumeMultiplier = targetVolumeMultiplier = 1f;
        currentCutoffFrequency = targetCutoffFrequency = clearCutoffFrequency;
        if (newsController) newsController.SetOcclusionMultiplier(1f);
#if UNITY_WEBGL && !UNITY_EDITOR
        if (lowPassFilter) lowPassFilter.enabled = false;
#else
        if (lowPassFilter)
        {
            lowPassFilter.enabled = true;
            lowPassFilter.cutoffFrequency = clearCutoffFrequency;
        }
#endif
    }

    private void Update()
    {
        if (!listenerTarget) ResolveListener();

        if (Time.unscaledTime >= nextCheckTime)
        {
            nextCheckTime = Time.unscaledTime + checkInterval;
            IsOccluded = HasRelevantObstacle();
            targetVolumeMultiplier = IsOccluded ? occludedVolumeMultiplier : 1f;
            targetCutoffFrequency = IsOccluded ? occludedCutoffFrequency : clearCutoffFrequency;
        }

        float blend = 1f - Mathf.Exp(-transitionResponse * Time.unscaledDeltaTime);
        currentVolumeMultiplier = Mathf.Lerp(currentVolumeMultiplier, targetVolumeMultiplier, blend);
        currentCutoffFrequency = Mathf.Lerp(currentCutoffFrequency, targetCutoffFrequency, blend);
        if (newsController) newsController.SetOcclusionMultiplier(currentVolumeMultiplier);
#if !UNITY_WEBGL || UNITY_EDITOR
        if (lowPassFilter) lowPassFilter.cutoffFrequency = currentCutoffFrequency;
#endif
    }

    private void ResolveListener()
    {
        AudioListener listener = FindFirstObjectByType<AudioListener>();
        if (listener) listenerTarget = listener.transform;
    }

    private bool HasRelevantObstacle()
    {
        if (!audioSource || !listenerTarget) return false;

        Vector3 origin = audioSource.transform.position;
        Vector3 destination = listenerTarget.position;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f) return false;

        int count = Physics.RaycastNonAlloc(
            origin,
            direction / distance,
            hits,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);

        for (int index = 0; index < count; index++)
        {
            Collider obstacle = hits[index].collider;
            if (!obstacle || ShouldIgnore(obstacle)) continue;
            return true;
        }

        return false;
    }

    private bool ShouldIgnore(Collider obstacle)
    {
        Transform obstacleTransform = obstacle.transform;
        if (playerRoot && obstacleTransform.IsChildOf(playerRoot)) return true;
        if (sourceHierarchyRoot && obstacleTransform.IsChildOf(sourceHierarchyRoot)) return true;
        if (obstacleTransform.IsChildOf(audioSource.transform)) return true;
        return obstacle.bounds.extents.magnitude < minimumObstacleExtent;
    }

    private void OnDisable()
    {
        if (newsController) newsController.SetOcclusionMultiplier(1f);
        if (lowPassFilter)
        {
            lowPassFilter.cutoffFrequency = clearCutoffFrequency;
            lowPassFilter.enabled = true;
        }
    }
}
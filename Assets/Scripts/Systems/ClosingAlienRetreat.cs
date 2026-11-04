using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ClosingAlienRetreat : MonoBehaviour
{
    [Header("Recorrido final")]
    [SerializeField] private Vector3 worldRetreatOffset = new Vector3(3.2f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float retreatSeconds = 2.2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine retreatRoutine;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool originCached;

    public bool IsMoving => retreatRoutine != null;
    public bool IsComplete { get; private set; }
    public float RetreatSeconds => retreatSeconds;
    public Vector3 WorldRetreatOffset => worldRetreatOffset;

    private void OnEnable()
    {
        CacheOrigin();
        ResetPose();
    }

    public void BeginRetreat()
    {
        CacheOrigin();
        if (retreatRoutine != null) StopCoroutine(retreatRoutine);
        ResetPose();
        retreatRoutine = StartCoroutine(Retreat());
    }

    private IEnumerator Retreat()
    {
        Vector3 destination = originPosition + worldRetreatOffset;
        for (float elapsed = 0f; elapsed < retreatSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float normalized = Mathf.Clamp01(elapsed / retreatSeconds);
            transform.position = Vector3.LerpUnclamped(originPosition, destination, movementCurve.Evaluate(normalized));
            yield return null;
        }

        transform.position = destination;
        IsComplete = true;
        retreatRoutine = null;
    }

    private void CacheOrigin()
    {
        if (originCached) return;
        originPosition = transform.position;
        originRotation = transform.rotation;
        originCached = true;
    }

    private void ResetPose()
    {
        if (!originCached) return;
        transform.SetPositionAndRotation(originPosition, originRotation);
        IsComplete = false;
    }

    private void OnDisable()
    {
        if (retreatRoutine != null) StopCoroutine(retreatRoutine);
        retreatRoutine = null;
        ResetPose();
    }
}

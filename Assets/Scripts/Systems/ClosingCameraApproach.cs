using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineFollow))]
public sealed class ClosingCameraApproach : MonoBehaviour
{
    [SerializeField] private CinemachineFollow follow;
    [SerializeField] private Vector3 startOffset = new Vector3(1.25f, 0.65f, 2.17f);
    [SerializeField] private Vector3 finalOffset = new Vector3(0.65f, 0.35f, 1.13f);
    [SerializeField, Min(0.1f)] private float approachSeconds = 1.8f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine approachRoutine;

    public bool IsMoving => approachRoutine != null;
    public bool IsComplete { get; private set; }
    public float ApproachSeconds => approachSeconds;

    private void Awake()
    {
        if (!follow) follow = GetComponent<CinemachineFollow>();
    }

    private void OnEnable()
    {
        if (!follow) follow = GetComponent<CinemachineFollow>();
        ResetApproach();
    }

    public void BeginApproach()
    {
        if (!follow) return;
        if (approachRoutine != null) StopCoroutine(approachRoutine);
        ResetApproach();
        approachRoutine = StartCoroutine(Approach());
    }

    private IEnumerator Approach()
    {
        for (float elapsed = 0f; elapsed < approachSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float normalized = Mathf.Clamp01(elapsed / approachSeconds);
            follow.FollowOffset = Vector3.LerpUnclamped(
                startOffset,
                finalOffset,
                movementCurve.Evaluate(normalized));
            yield return null;
        }

        follow.FollowOffset = finalOffset;
        IsComplete = true;
        approachRoutine = null;
    }

    private void ResetApproach()
    {
        if (follow) follow.FollowOffset = startOffset;
        IsComplete = false;
    }

    private void OnDisable()
    {
        if (approachRoutine != null) StopCoroutine(approachRoutine);
        approachRoutine = null;
        ResetApproach();
    }
}
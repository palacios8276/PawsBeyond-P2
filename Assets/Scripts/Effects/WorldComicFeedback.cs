using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WorldComicFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Vector3 offset;
    [SerializeField, Min(0.1f)] private float duration = 0.8f;
    [SerializeField, Min(0.01f)] private float size = 0.45f;
    [SerializeField] private bool followAnchor;
    [SerializeField] private bool cameraRelativeOffset;
    private Coroutine animationRoutine;

    private void Awake() { if (label) label.enabled = false; }
    public void Show() => ShowAt(transform.position);
    public void ShowMessage(string message)
    {
        if (!label) return;
        label.text = message;
        Show();
    }
    public void ShowAt(Vector3 position)
    {
        if (!label || !isActiveAndEnabled) return;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(Animate(position));
    }

    private IEnumerator Animate(Vector3 position)
    {
        label.transform.SetParent(null, true);
        label.transform.position = position + offset;
        label.enabled = true;
        Color color = label.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!label) yield break;
            Vector3 worldOffset = offset;
            if (cameraRelativeOffset && Camera.main)
            {
                var view = Camera.main.transform;
                worldOffset = view.right * offset.x + Vector3.up * offset.y
                    + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * offset.z;
            }
            label.transform.position = (followAnchor ? transform.position : position) + worldOffset;
            float progress = t / duration;
            float pop = Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Min(progress / 0.2f, 1f)));
            label.transform.localScale = Vector3.one * (size * pop);
            label.color = new Color(color.r, color.g, color.b, 1f - Mathf.SmoothStep(0.35f, 1f, progress));
            if (Camera.main) label.transform.rotation = Camera.main.transform.rotation;
            yield return null;
        }
        if (label) label.enabled = false;
        animationRoutine = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        animationRoutine = null;
        if (label) label.enabled = false;
    }
}

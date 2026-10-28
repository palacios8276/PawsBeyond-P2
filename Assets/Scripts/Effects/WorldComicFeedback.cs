using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WorldComicFeedback : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Vector3 offset;
    [SerializeField, Min(0.1f)] private float duration = 0.8f;
    [SerializeField, Min(0.01f)] private float size = 0.45f;
    private Coroutine animationRoutine;

    private void Awake() { if (label) label.enabled = false; }
    public void Show() => ShowAt(transform.position);
    public void ShowAt(Vector3 position)
    {
        if (!label || !isActiveAndEnabled) return;
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(Animate(position + offset));
    }

    private IEnumerator Animate(Vector3 position)
    {
        label.transform.SetParent(null, true);
        label.transform.position = position;
        label.enabled = true;
        Color color = label.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!label) yield break;
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

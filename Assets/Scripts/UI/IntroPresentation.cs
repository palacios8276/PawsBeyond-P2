using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class IntroPresentation : MonoBehaviour
{
    [SerializeField] private CanvasGroup coverGroup;
    [SerializeField, Min(0.1f)] private float fadeInSeconds = 0.75f;
    [SerializeField, Min(0f)] private float holdSeconds = 4.5f;
    [SerializeField, Min(0.1f)] private float fadeOutSeconds = 0.75f;
    [SerializeField] private string gameplaySceneName = "Playground";

    public float TotalDuration => fadeInSeconds + holdSeconds + fadeOutSeconds;

    private void Awake()
    {
        if (!coverGroup) coverGroup = GetComponent<CanvasGroup>();
        coverGroup.alpha = 0f;
        coverGroup.interactable = false;
        coverGroup.blocksRaycasts = false;
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return Fade(0f, 1f, fadeInSeconds);
        if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);
        yield return Fade(1f, 0f, fadeOutSeconds);
        yield return SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        coverGroup.alpha = from;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            coverGroup.alpha = Mathf.SmoothStep(from, to, elapsed / seconds);
            yield return null;
        }
        coverGroup.alpha = to;
    }
}
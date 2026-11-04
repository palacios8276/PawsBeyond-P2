using UnityEngine;

// Adapta el evento C# existente del accidente a la autoridad musical sin tocar el puzzle.
[DisallowMultipleComponent]
public sealed class BathroomMusicCue : MonoBehaviour
{
    [SerializeField] private BathroomBasketDrop puzzle;
    [SerializeField] private AdaptiveMusicController musicState;

    private void OnEnable()
    {
        if (puzzle) puzzle.AccidentStarted += HandleAccidentStarted;
    }

    private void OnDisable()
    {
        if (puzzle) puzzle.AccidentStarted -= HandleAccidentStarted;
    }

    private void HandleAccidentStarted()
    {
        if (musicState) musicState.OnThreatStarted();
    }
}

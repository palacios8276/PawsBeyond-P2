using System.Collections;
using UnityEngine;

public sealed class BathroomAccident : MonoBehaviour
{
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private Transform lightShelf;
    [SerializeField] private AudioSource contactAudio;
    [SerializeField] private Vector3 reactionOffset = new Vector3(-1.2f, 0f, -0.6f);
    [SerializeField, Min(0.1f)] private float reactionSeconds = 0.5f;
    [UnityEngine.Serialization.FormerlySerializedAs("tipSeconds")]
    [SerializeField, Min(0.1f)] private float shakeSeconds = 0.25f;
    [SerializeField, Min(0.1f)] private float recoverySeconds = 0.65f;
    [SerializeField, Min(0f)] private float shakeDistance = 0.025f;
    private Vector3 restPosition;
    private bool reacting, shelfShaking;
    private Vector3 shelfRestPosition;

    public IEnumerator ReactAndShake()
    {
        restPosition = journey.transform.position;
        reacting = true;
        journey.SetNarrativeHold(true);
        Vector3 contactPosition = restPosition + reactionOffset;
        for (float t = 0f; t < reactionSeconds; t += Time.deltaTime)
        {
            journey.transform.position = Vector3.Lerp(restPosition, contactPosition,
                Mathf.SmoothStep(0f, 1f, t / reactionSeconds));
            yield return null;
        }
        journey.transform.position = contactPosition;
        if (contactAudio) contactAudio.Play();
        shelfRestPosition = lightShelf.localPosition;
        shelfShaking = true;
        for (float t = 0f; t < shakeSeconds; t += Time.deltaTime)
        {
            float progress = t / shakeSeconds;
            float shake = Mathf.Sin(progress * Mathf.PI * 4f) * Mathf.Sin(progress * Mathf.PI);
            lightShelf.localPosition = shelfRestPosition + Vector3.right * (shake * shakeDistance);
            yield return null;
        }
        lightShelf.localPosition = shelfRestPosition;
        shelfShaking = false;
        StartCoroutine(RecoverOwners(contactPosition));
    }

    private IEnumerator RecoverOwners(Vector3 from)
    {
        for (float t = 0f; t < recoverySeconds; t += Time.deltaTime)
        {
            if (!journey) yield break;
            journey.transform.position = Vector3.Lerp(from, restPosition,
                Mathf.SmoothStep(0f, 1f, t / recoverySeconds));
            yield return null;
        }
        if (journey) { journey.transform.position = restPosition; journey.SetNarrativeHold(false); }
        reacting = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (shelfShaking && lightShelf) lightShelf.localPosition = shelfRestPosition;
        shelfShaking = false;
        if (reacting && journey) { journey.transform.position = restPosition; journey.SetNarrativeHold(false); }
        reacting = false;
    }
}

using System.Collections;
using UnityEngine;

public sealed class BathroomAccident : MonoBehaviour
{
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private Transform lightShelf;
    [SerializeField] private Collider shelfCollider;
    [SerializeField] private AudioSource contactAudio;
    [SerializeField] private Vector3 reactionOffset = new Vector3(-1.2f, 0f, -0.6f);
    [SerializeField, Min(0.1f)] private float reactionSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float tipSeconds = 0.25f;
    [SerializeField, Min(0.1f)] private float recoverySeconds = 0.65f;
    [SerializeField] private Vector3 shelfTip = new Vector3(0f, 0f, -22f);
    private Vector3 restPosition;
    private bool reacting;

    public IEnumerator ReactAndTip()
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
        Quaternion restRotation = lightShelf.localRotation;
        for (float t = 0f; t < tipSeconds; t += Time.deltaTime)
        {
            float amount = Mathf.SmoothStep(0f, 1f, t / tipSeconds);
            lightShelf.localRotation = restRotation * Quaternion.Euler(shelfTip * amount);
            yield return null;
        }
        lightShelf.localRotation = restRotation * Quaternion.Euler(shelfTip);
        if (shelfCollider) shelfCollider.enabled = false;
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
        if (reacting && journey) { journey.transform.position = restPosition; journey.SetNarrativeHold(false); }
        reacting = false;
    }
}

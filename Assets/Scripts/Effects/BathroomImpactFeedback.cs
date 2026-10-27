using System.Collections;
using TMPro;
using UnityEngine;

public sealed class BathroomImpactFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private TMP_Text impactText;
    [SerializeField] private Vector3 textOffset = new Vector3(0f, 0.8f, -0.7f);
    [SerializeField, Min(0.1f)] private float textSeconds = 0.8f;
    [SerializeField, Min(0.01f)] private float textScale = 0.38f;
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.6f;
    private bool armed, played;
    public bool HasPlayed => played;
    public void Arm() => armed = true;

    private void Awake() { if (impactText) impactText.enabled = false; }

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed || played || collision.relativeVelocity.sqrMagnitude < minimumImpactSpeed * minimumImpactSpeed) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.normal.y < 0.5f) continue;
            played = true;
            if (impactAudio) impactAudio.Play();
            if (impactText) StartCoroutine(ShowImpact(contact.point));
            return;
        }
    }

    private IEnumerator ShowImpact(Vector3 position)
    {
        impactText.transform.SetParent(null, true);
        impactText.transform.position = position + textOffset;
        impactText.enabled = true;
        Color color = impactText.color;
        for (float t = 0f; t < textSeconds; t += Time.deltaTime)
        {
            if (!impactText) yield break;
            float progress = t / textSeconds;
            float pop = Mathf.Lerp(0.35f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Min(progress / 0.2f, 1f)));
            impactText.transform.localScale = Vector3.one * (textScale * pop);
            impactText.color = new Color(color.r, color.g, color.b, 1f - Mathf.SmoothStep(0.35f, 1f, progress));
            var camera = Camera.main;
            if (camera) impactText.transform.rotation = camera.transform.rotation;
            yield return null;
        }
        if (impactText) impactText.enabled = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (impactText) impactText.enabled = false;
    }
}

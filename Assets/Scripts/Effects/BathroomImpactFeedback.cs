using UnityEngine;

public sealed class BathroomImpactFeedback : MonoBehaviour
{
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private WorldComicFeedback comic;
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.6f;
    private bool armed, played;
    public bool HasPlayed => played;
    public void Arm() => armed = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (!armed || played || collision.relativeVelocity.sqrMagnitude < minimumImpactSpeed * minimumImpactSpeed) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            if (contact.normal.y < 0.5f) continue;
            played = true;
            if (impactAudio) impactAudio.Play();
            if (comic) comic.ShowAt(contact.point);
            return;
        }
    }
}

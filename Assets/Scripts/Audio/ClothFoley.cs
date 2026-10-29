using UnityEngine;

[RequireComponent(typeof(BasketDrag), typeof(AudioSource))]
public sealed class ClothFoley : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] rustles;
    [SerializeField, Min(0.1f)] private float carryInterval = 0.65f;
    [SerializeField, Min(0f)] private float movementThreshold = 0.12f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    private BasketDrag drag;
    private bool wasHeld;
    private Vector3 lastPosition;
    private float nextRustle;
    private int variant;

    private void Awake()
    {
        drag = GetComponent<BasketDrag>();
        if (!source) source = GetComponent<AudioSource>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        bool held = drag.IsHeld;
        if (held != wasHeld)
        {
            Rustle(held ? 0.8f : 0.6f);
            nextRustle = Time.time + carryInterval;
            lastPosition = transform.position;
        }
        else if (held && Time.time >= nextRustle && Vector3.Distance(lastPosition, transform.position) > movementThreshold)
        {
            Rustle(0.3f);
            lastPosition = transform.position;
            nextRustle = Time.time + carryInterval;
        }
        wasHeld = held;
    }

    private void Rustle(float gain)
    {
        if (!source || rustles == null || rustles.Length == 0) return;
        var clip = rustles[variant++ % rustles.Length];
        if (!clip) return;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, gain);
    }
}

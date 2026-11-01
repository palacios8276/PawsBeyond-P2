using UnityEngine;

// Mantiene una respiración discreta y responde al desplazamiento real del CharacterController.
[RequireComponent(typeof(AudioSource))]
public sealed class DogBreathingAudio : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float restingVolume = 0.08f;
    [SerializeField, Range(0f, 1f)] private float movingVolume = 0.15f;
    [SerializeField, Min(0.1f)] private float fullMovementSpeed = 4f;
    [SerializeField, Min(0.05f)] private float volumeResponse = 1.4f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.96f, 1.04f);

    private Vector3 previousPosition;
    private float volumeVelocity;
    public float CurrentMovement01 { get; private set; }

    private void Awake()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (!controller) controller = GetComponentInParent<CharacterController>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        previousPosition = controller ? controller.transform.position : transform.position;
        if (audioSource && audioSource.clip && !audioSource.isPlaying) audioSource.Play();
    }

    private void Update()
    {
        if (!controller || !audioSource) return;
        Vector3 position = controller.transform.position;
        Vector3 delta = Vector3.ProjectOnPlane(position - previousPosition, Vector3.up);
        previousPosition = position;
        float speed = Time.deltaTime > 0f && delta.magnitude < 2f ? delta.magnitude / Time.deltaTime : 0f;
        CurrentMovement01 = Mathf.Clamp01(speed / fullMovementSpeed);
        float target = Mathf.Lerp(restingVolume, movingVolume, CurrentMovement01);
        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, target, ref volumeVelocity, volumeResponse);
        audioSource.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, CurrentMovement01);
        if (audioSource.clip && !audioSource.isPlaying) audioSource.Play();
    }

    private void OnDisable()
    {
        if (audioSource) audioSource.Stop();
        CurrentMovement01 = 0f;
        volumeVelocity = 0f;
    }
}

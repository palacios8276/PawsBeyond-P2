using UnityEngine;

// Observa el desplazamiento; nunca mueve al jugador ni modifica sus controles.
public sealed class DogFootstepAudio : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] variants;
    [SerializeField, Min(0.1f)] private float stepDistance = 0.8f;
    [SerializeField, Min(0.1f)] private float runningSpeedThreshold = 3.5f;
    [SerializeField, Range(0.4f, 1f)] private float runningStepDistanceMultiplier = 0.72f;
    [SerializeField, Min(0.05f)] private float minimumInterval = 0.16f;
    [SerializeField, Min(0f)] private float minimumSpeed = 0.12f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    private Vector3 previousPosition;
    private float distance, nextStepTime, baseVolume;
    private int lastVariant = -1;
    private readonly System.Random random = new System.Random();
    public int PlayedSteps { get; private set; }

    private void OnEnable()
    {
        if (controller) previousPosition = controller.transform.position;
        distance = 0f;
        if (audioSource) baseVolume = audioSource.volume;
    }

    private void LateUpdate()
    {
        if (!controller || !audioSource) return;
        Vector3 position = controller.transform.position;
        Vector3 delta = position - previousPosition;
        previousPosition = position;
        delta.y = 0f;
        float travel = delta.magnitude;
        float speed = Time.deltaTime > 0f ? travel / Time.deltaTime : 0f;
        // Ignora reposo, caída y traslados bruscos de prueba/carga de escena.
        if (Time.deltaTime <= 0f || !controller.isGrounded || travel > 2f
            || speed < minimumSpeed)
        {
            distance = 0f;
            if (audioSource.isPlaying) audioSource.Stop();
            return;
        }
        distance += travel;
        float currentStepDistance = speed >= runningSpeedThreshold
            ? stepDistance * runningStepDistanceMultiplier : stepDistance;
        if (distance < currentStepDistance || Time.time < nextStepTime || audioSource.isPlaying
            || variants == null || variants.Length == 0) return;
        int index = variants.Length == 1 ? 0 : lastVariant < 0 ? random.Next(variants.Length)
            : (lastVariant + 1 + random.Next(variants.Length - 1)) % variants.Length;
        if (!variants[index]) return;
        lastVariant = index;
        distance %= currentStepDistance;
        nextStepTime = Time.time + minimumInterval;
        audioSource.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, (float)random.NextDouble());
        audioSource.volume = baseVolume * Mathf.Lerp(0.94f, 1.06f, (float)random.NextDouble());
        audioSource.clip = variants[index];
        audioSource.Play();
        PlayedSteps++;
    }

    private void OnDisable()
    {
        if (audioSource) { audioSource.Stop(); audioSource.volume = baseVolume; }
        distance = 0f;
    }
}

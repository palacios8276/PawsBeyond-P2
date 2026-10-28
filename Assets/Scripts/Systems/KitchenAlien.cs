using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum KitchenAlienState { Inactive, Appearing, Throwing, Provoking, WaitingForBark, Startled, ReleasingShutter, Running, Repairing, Finished }

[RequireComponent(typeof(CharacterController))]
public sealed class KitchenAlien : MonoBehaviour
{
    [SerializeField] private Transform visual, leftArm, rightArm, hand, listeningPoint;
    [SerializeField] private Transform attentionTarget;
    [SerializeField, Min(0.1f)] private float runSpeed = 6.5f;
    [SerializeField, Min(0.1f)] private float startleSeconds = 0.45f;
    [SerializeField] private UnityEvent onStateChanged = new UnityEvent();
    [SerializeField] private UnityEvent onToyReleased = new UnityEvent();
    private CharacterController controller;
    private Vector3 visualStart;
    public KitchenAlienState State { get; private set; }
    public float RunSpeed => runSpeed;
    public Transform ListeningPoint => listeningPoint ? listeningPoint : transform;
    public int Throws { get; private set; }
    public Rigidbody ThrownToy { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        visualStart = visual.localPosition;
    }

    public void SetState(KitchenAlienState state)
    {
        if (State == state) return;
        State = state;
        onStateChanged.Invoke();
    }

    public IEnumerator Provoke(Rigidbody prefab, Transform landing)
    {
        SetState(KitchenAlienState.Appearing);
        yield return new WaitForSeconds(0.6f);
        SetState(KitchenAlienState.Throwing);
        yield return new WaitForSeconds(0.35f);
        if (Throws == 0 && prefab && landing)
        {
            ThrownToy = Instantiate(prefab, hand.position, Quaternion.identity);
            ThrownToy.isKinematic = false;
            float flight = 0.55f;
            ThrownToy.linearVelocity = (landing.position - hand.position) / flight - Physics.gravity * (flight * 0.5f);
            Throws++;
            onToyReleased.Invoke();
        }
        SetState(KitchenAlienState.Provoking);
    }

    public IEnumerator Startle()
    {
        SetState(KitchenAlienState.Startled);
        yield return new WaitForSeconds(startleSeconds);
    }

    public IEnumerator Travel(Transform[] route, bool avoidPlayer = false)
    {
        SetState(KitchenAlienState.Running);
        foreach (var point in route)
        {
            if (!point) continue;
            while (Vector3.ProjectOnPlane(point.position - transform.position, Vector3.up).sqrMagnitude > 0.012f)
            {
                Vector3 direction = Vector3.ProjectOnPlane(point.position - transform.position, Vector3.up);
                if (avoidPlayer && attentionTarget)
                {
                    Vector3 occupiedPoint = Vector3.ProjectOnPlane(attentionTarget.position - point.position, Vector3.up);
                    // Los puntos de salida son guía, no mecanismos que deban tocarse exactamente.
                    if (occupiedPoint.sqrMagnitude < 0.64f && direction.sqrMagnitude < 1f) break;
                    if (Physics.CapsuleCast(transform.position + Vector3.up * 0.3f,
                        transform.position + Vector3.up * 1.05f, 0.24f, direction.normalized,
                        out RaycastHit blocker, 0.8f, ~0, QueryTriggerInteraction.Ignore)
                        && blocker.collider.transform.root.CompareTag("Player"))
                        direction = direction.normalized * 0.25f + Vector3.Cross(Vector3.up, direction.normalized);
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 14f);
                controller.Move(Vector3.ClampMagnitude(direction, runSpeed * Time.deltaTime) + Vector3.down * Time.deltaTime);
                yield return null;
            }
        }
    }

    private void Update()
    {
        if (State == KitchenAlienState.Inactive) return;
        if (attentionTarget && (State == KitchenAlienState.Provoking || State == KitchenAlienState.WaitingForBark || State == KitchenAlienState.Appearing))
        {
            Vector3 direction = Vector3.ProjectOnPlane(attentionTarget.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
        bool running = State == KitchenAlienState.Running;
        float wave = Mathf.Sin(Time.time * (running ? 16f : 4f));
        visual.localPosition = visualStart + Vector3.up * (running ? Mathf.Abs(wave) * 0.045f : 0f);
        float lean = State == KitchenAlienState.Repairing ? 18f : State == KitchenAlienState.Startled ? -18f : running ? 12f : 0f;
        visual.localRotation = Quaternion.Euler(lean, 0f, 0f);
        float arms = State == KitchenAlienState.Startled ? -145f : State == KitchenAlienState.Repairing ? -65f + wave * 12f
            : State == KitchenAlienState.ReleasingShutter ? -80f : State == KitchenAlienState.Throwing ? -125f : running ? wave * 40f : -12f;
        leftArm.localRotation = Quaternion.Euler(arms, 0f, -18f);
        rightArm.localRotation = Quaternion.Euler(running ? -arms : arms, 0f, 18f);
    }
}



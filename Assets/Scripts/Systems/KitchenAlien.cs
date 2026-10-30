using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum KitchenAlienState { Inactive, Appearing, Throwing, Provoking, WaitingForBark, Startled, ReleasingShutter, Running, Repairing, Finished, Hiding, Crawling, ThrowingOtherHand }

[RequireComponent(typeof(CharacterController))]
public sealed class KitchenAlien : MonoBehaviour
{
    [SerializeField] private Transform visual, leftArm, rightArm, hand, listeningPoint;
    [SerializeField] private Transform attentionTarget;
    [SerializeField] private Transform repairTarget;
    [SerializeField] private Transform otherHand;
    [SerializeField] private RemoteControlFeedback remoteControl;
    [SerializeField, Min(0.1f)] private float runSpeed = 6.5f;
    [SerializeField, Min(0.1f)] private float startleSeconds = 0.45f;
    [SerializeField] private UnityEvent onStateChanged = new UnityEvent();
    [SerializeField] private UnityEvent onToyReleased = new UnityEvent();
    [Header("Sincronización audiovisual por acción, sin clips obligatorios")]
    [SerializeField] private UnityEvent onAppeared = new UnityEvent();
    [SerializeField] private UnityEvent onStartled = new UnityEvent();
    [SerializeField] private UnityEvent onRunStarted = new UnityEvent();
    [SerializeField] private UnityEvent onRepairStarted = new UnityEvent();
    [SerializeField] private UnityEvent onRetreated = new UnityEvent();
    [SerializeField] private UnityEvent onBottleReleased = new UnityEvent();
    [SerializeField] private UnityEvent onBarkReaction = new UnityEvent();
    [Header("Retirada física exterior")]
    [SerializeField, Min(0.1f)] private float exteriorRetreatSeconds = 2f;
    [SerializeField, Min(0.1f)] private float exteriorRetreatSpeed = 4f;
    private CharacterController controller;
    private Vector3 visualStart;
    private float recoilUntil;
    public KitchenAlienState State { get; private set; }
    public float RunSpeed => runSpeed;
    public Transform ListeningPoint => listeningPoint ? listeningPoint : transform;
    public int Throws { get; private set; }
    public Rigidbody ThrownToy { get; private set; }
    public void ReactToBark() { recoilUntil = Time.time + 0.25f; onBarkReaction.Invoke(); }
    public void SetRemoteActive(bool active) { if (remoteControl) remoteControl.SetOperating(active); }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        visualStart = visual.localPosition;
    }

    public void SetState(KitchenAlienState state)
    {
        if (State == state) return;
        var previous = State;
        State = state;
        onStateChanged.Invoke();
        if (state == KitchenAlienState.Appearing || previous == KitchenAlienState.Inactive && state == KitchenAlienState.WaitingForBark) onAppeared.Invoke();
        if (state == KitchenAlienState.Startled) onStartled.Invoke();
        if (state == KitchenAlienState.Running) onRunStarted.Invoke();
        if (state == KitchenAlienState.Repairing) onRepairStarted.Invoke();
        if (state == KitchenAlienState.Finished) onRetreated.Invoke();
    }

    public void FacePoint(Vector3 point)
    {
        Vector3 direction = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
    }

    public IEnumerator Provoke(Rigidbody prefab, Transform landing)
    {
        SetState(KitchenAlienState.Appearing);
        yield return new WaitForSeconds(0.6f);
        SetState(KitchenAlienState.Throwing);
        yield return new WaitForSeconds(0.35f);
        if (Throws == 0 && prefab && landing)
        {
            ThrownToy = Instantiate(prefab, hand.position + transform.forward * 0.28f, Quaternion.identity);
            ThrownToy.isKinematic = false;
            float flight = 0.55f;
            ThrownToy.linearVelocity = (landing.position - ThrownToy.position) / flight - Physics.gravity * (flight * 0.5f);
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

    public IEnumerator DistractWithOtherHand(Rigidbody prefab, Transform landing)
    {
        SetState(KitchenAlienState.Appearing);
        yield return new WaitForSeconds(0.45f);
        SetState(KitchenAlienState.ThrowingOtherHand);
        yield return new WaitForSeconds(0.35f);
        if (Throws == 0 && prefab && landing)
        {
            Transform release = otherHand ? otherHand : hand;
            ThrownToy = Instantiate(prefab, release.position + transform.forward * 0.3f, Quaternion.identity);
            ThrownToy.isKinematic = false;
            float flight = 0.6f;
            ThrownToy.linearVelocity = (landing.position - ThrownToy.position) / flight - Physics.gravity * (flight * 0.5f);
            Throws++;
            onBottleReleased.Invoke();
        }
    }

    public IEnumerator EscapeWindow(Transform staging, Transform outside, Transform departure,
        Collider passageBlocker = null)
    {
        SetState(KitchenAlienState.Crawling);
        float height = controller.height;
        Vector3 center = controller.center;
        float step = controller.stepOffset;
        if (passageBlocker) Physics.IgnoreCollision(controller, passageBlocker, true);
        controller.height = 0.7f; controller.center = Vector3.up * 0.35f; controller.stepOffset = 0.1f;
        visual.localScale = new Vector3(1f, 0.55f, 1f);
        foreach (var target in new[] { staging, outside, departure })
        {
            while (Vector3.Distance(transform.position, target.position) > 0.04f)
            {
                Vector3 delta = target.position - transform.position;
                Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
                if (flat.sqrMagnitude > 0.01f) FacePoint(target.position);
                controller.Move(Vector3.ClampMagnitude(delta, 2f * Time.deltaTime));
                yield return null;
            }
        }
        visual.localScale = Vector3.one;
        controller.height = height; controller.center = center; controller.stepOffset = step;
        if (passageBlocker) Physics.IgnoreCollision(controller, passageBlocker, false);
    }

    public IEnumerator RetreatExterior(Transform destination)
    {
        SetState(KitchenAlienState.Running);
        float started = Time.time;
        while (Vector3.ProjectOnPlane(destination.position - transform.position, Vector3.up).sqrMagnitude > 0.012f
            || Time.time - started < exteriorRetreatSeconds || VisibleToPlayer())
        {
            Vector3 direction = Vector3.ProjectOnPlane(destination.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.012f)
            {
                FacePoint(destination.position);
                controller.Move(Vector3.ClampMagnitude(direction, exteriorRetreatSpeed * Time.deltaTime));
            }
            yield return null;
        }
        SetState(KitchenAlienState.Finished);
        gameObject.SetActive(false);
    }

    private bool VisibleToPlayer()
    {
        Camera camera = Camera.main;
        if (!camera) return true;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            Bounds bounds = renderer.bounds;
            if (!renderer.enabled || !GeometryUtility.TestPlanesAABB(planes, bounds)) continue;
            // Revisar centro y esquinas evita retirar una silueta todavía visible en el marco.
            for (int i = -1; i < 8; i++)
            {
                Vector3 sample = i < 0 ? bounds.center : bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 viewport = camera.WorldToViewportPoint(sample);
                if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) continue;
                if (!Physics.Linecast(camera.transform.position, sample, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    || hit.collider.transform.IsChildOf(transform)) return true;
            }
        }
        return false;
    }
    public IEnumerator Travel(Transform[] route, bool avoidCharacters = false, bool optionalWaypoints = false)
    {
        SetState(KitchenAlienState.Running);
        foreach (var point in route)
        {
            if (!point) continue;
            while (Vector3.ProjectOnPlane(point.position - transform.position, Vector3.up).sqrMagnitude > 0.012f)
            {
                Vector3 direction = Vector3.ProjectOnPlane(point.position - transform.position, Vector3.up);
                if (avoidCharacters && attentionTarget)
                {
                    Vector3 occupiedPoint = Vector3.ProjectOnPlane(attentionTarget.position - point.position, Vector3.up);
                    // Los puntos de salida son guía, no mecanismos que deban tocarse exactamente.
                    if (optionalWaypoints && occupiedPoint.sqrMagnitude < 0.64f && direction.sqrMagnitude < 1f) break;
                    if (Physics.CapsuleCast(transform.position + Vector3.up * 0.3f,
                        transform.position + Vector3.up * 1.05f, 0.24f, direction.normalized,
                        out RaycastHit blocker, 0.8f, ~0, QueryTriggerInteraction.Ignore)
                        && blocker.collider.GetComponentInParent<CharacterController>()
                        && blocker.collider.GetComponentInParent<CharacterController>() != controller)
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
        if (State == KitchenAlienState.Repairing && repairTarget) FacePoint(repairTarget.position);
        if (attentionTarget && (State == KitchenAlienState.Provoking || State == KitchenAlienState.WaitingForBark || State == KitchenAlienState.Appearing))
        {
            Vector3 direction = Vector3.ProjectOnPlane(attentionTarget.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
        bool running = State == KitchenAlienState.Running;
        float wave = Mathf.Sin(Time.time * (running ? 16f : 4f));
        visual.localPosition = visualStart + Vector3.up * (running ? Mathf.Abs(wave) * 0.045f : 0f);
        float lean = State == KitchenAlienState.Crawling ? 15f : State == KitchenAlienState.Repairing ? 8f
            : State == KitchenAlienState.Startled || Time.time < recoilUntil ? -18f : running ? 12f : 0f;
        visual.localRotation = Quaternion.Euler(lean, 0f, 0f);
        float arms = State == KitchenAlienState.Startled ? -145f : State == KitchenAlienState.Repairing ? -65f + wave * 12f
            : State == KitchenAlienState.ReleasingShutter ? -80f : State == KitchenAlienState.Throwing ? -125f : running ? wave * 40f : -12f;
        leftArm.localRotation = Quaternion.Euler(arms, 0f, -18f);
        rightArm.localRotation = Quaternion.Euler(running ? -arms : arms, 0f, 18f);
        if (State == KitchenAlienState.ThrowingOtherHand)
        { leftArm.localRotation = Quaternion.Euler(-125f, 0f, -18f); rightArm.localRotation = Quaternion.Euler(-65f, 0f, 18f); }
    }
}



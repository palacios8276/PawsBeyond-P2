using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasketDrag : MonoBehaviour
{
    [SerializeField] private float followStrength = 12f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float carryHeight = 0.55f;

    private Rigidbody body;
    private Transform holder;
    private Vector3 localGrip;
    private bool useExactGrip;
    private Collider[] playerColliders;
    private Collider[] basketColliders;
    private CollisionDetectionMode previousCollisionMode;
    public bool IsHeld => holder != null;
    public bool HasBeenDragged { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        basketColliders = GetComponentsInChildren<Collider>();
    }

    public bool Grab(Transform player)
    {
        return Grab(player, null);
    }

    public bool Grab(Transform player, Transform grip)
    {
        if (body.isKinematic || IsHeld) return false;
        // Pulling frees clothes snagged in the doorway.
        body.constraints &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);
        holder = grip ? grip : player;
        useExactGrip = grip;
        localGrip = useExactGrip ? Vector3.zero : player.InverseTransformPoint(body.position);
        playerColliders = player.GetComponentsInChildren<Collider>();
        SetPlayerCollisions(true);
        previousCollisionMode = body.collisionDetectionMode;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        HasBeenDragged = true;
        return true;
    }

    public void Release()
    {
        SetPlayerCollisions(false);
        if (body) body.collisionDetectionMode = previousCollisionMode;
        holder = null;
        useExactGrip = false;
        playerColliders = null;
    }

    private void SetPlayerCollisions(bool ignore)
    {
        if (playerColliders == null || basketColliders == null) return;
        foreach (var a in basketColliders)
            foreach (var b in playerColliders)
                if (a && b) Physics.IgnoreCollision(a, b, ignore);
    }

    private void FixedUpdate()
    {
        if (!holder) return;
        Vector3 target = useExactGrip ? holder.position : holder.TransformPoint(localGrip);
        if (!useExactGrip) target.y = holder.position.y + carryHeight;
        Vector3 delta = target - body.position;
        // Los contactos pueden retrasar el objeto, pero solo Interact debe soltarlo.
        // La velocidad limitada y ContinuousDynamic conservan paredes y muebles sólidos.
        Vector3 velocity = Vector3.ClampMagnitude(delta * followStrength, maxSpeed);
        Vector3 current = body.linearVelocity;
        body.AddForce((velocity - current) * followStrength - Physics.gravity,
            ForceMode.Acceleration);
    }

    private void OnDisable() => Release();
}

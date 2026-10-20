using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasketDrag : MonoBehaviour
{
    [SerializeField] private float followStrength = 12f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float breakDistance = 4f;
    [SerializeField] private float carryHeight = 0.55f;

    private Rigidbody body;
    private Transform holder;
    private Vector3 localGrip;
    private Collider[] playerColliders;
    private Collider[] basketColliders;
    public bool IsHeld => holder != null;
    public bool HasBeenDragged { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        basketColliders = GetComponentsInChildren<Collider>();
    }

    public bool Grab(Transform player)
    {
        if (body.isKinematic || IsHeld) return false;
        // Pulling frees clothes snagged in the doorway.
        body.constraints &= ~(RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ);
        holder = player;
        localGrip = player.InverseTransformPoint(body.position);
        playerColliders = player.GetComponentsInChildren<Collider>();
        SetPlayerCollisions(true);
        HasBeenDragged = true;
        return true;
    }

    public void Release()
    {
        SetPlayerCollisions(false);
        holder = null;
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
        Vector3 target = holder.TransformPoint(localGrip);
        target.y = holder.position.y + carryHeight;
        Vector3 delta = target - body.position;
        if (delta.sqrMagnitude > breakDistance * breakDistance)
        {
            Release();
            return;
        }
        // Lift smoothly over the rim with forces; never teleport the garment.
        Vector3 velocity = Vector3.ClampMagnitude(delta * followStrength, maxSpeed);
        Vector3 current = body.linearVelocity;
        body.AddForce((velocity - current) * followStrength - Physics.gravity,
            ForceMode.Acceleration);
    }

    private void OnDisable() => Release();
}

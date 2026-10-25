using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BasketDrag))]
public sealed class BedroomToyPhysics : MonoBehaviour
{
    [Header("Empuje específico del juguete")]
    [SerializeField, Range(0f, 1f)] private float pushImpulseScale = 0.035f;
    [SerializeField, Min(0.01f)] private float pushInterval = 0.12f;
    [SerializeField, Min(0.1f)] private float looseHorizontalSpeed = 1.8f;
    [Header("Solo juguetes de práctica, sin barreras")]
    [SerializeField] private bool keepInBedroom = true;
    [SerializeField] private Vector3 roomCenter = new Vector3(-18.03f, 0f, -0.05f);
    [SerializeField] private Vector2 roomHalfSize = new Vector2(5.65f, 3.5f);
    [SerializeField, Min(0.1f)] private float edgeSoftness = 0.5f;
    [SerializeField, Min(0f)] private float edgeAcceleration = 4f;
    [SerializeField, Min(0.5f)] private float lostDistance = 2f;
    [SerializeField, Min(0.1f)] private float lostDelay = 1f;
    private Rigidbody body;
    private BasketDrag drag;
    private Vector3 home;
    private float nextPush, lostSince = -1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>(); drag = GetComponent<BasketDrag>(); home = body.position;
    }

    public void ApplyPlayerPush(Vector3 direction, Vector3 point, float power)
    {
        if (!isActiveAndEnabled || body.isKinematic || drag.IsHeld || Time.time < nextPush) return;
        nextPush = Time.time + pushInterval;
        body.AddForceAtPosition(direction * power * pushImpulseScale, point, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (body.isKinematic || drag.IsHeld) { lostSince = -1f; return; }
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = Vector3.ClampMagnitude(new Vector3(velocity.x, 0f, velocity.z), looseHorizontalSpeed);
        body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        if (!keepInBedroom) return;
        Vector3 offset = body.position - roomCenter;
        Vector3 inward = new Vector3(
            -Mathf.Sign(offset.x) * Mathf.Clamp01((Mathf.Abs(offset.x) - roomHalfSize.x + edgeSoftness) / edgeSoftness), 0f,
            -Mathf.Sign(offset.z) * Mathf.Clamp01((Mathf.Abs(offset.z) - roomHalfSize.y + edgeSoftness) / edgeSoftness));
        body.AddForce(inward * edgeAcceleration, ForceMode.Acceleration);
        bool lost = body.position.y < roomCenter.y - 1f || Mathf.Abs(offset.x) > roomHalfSize.x + lostDistance
            || Mathf.Abs(offset.z) > roomHalfSize.y + lostDistance;
        if (!lost) { lostSince = -1f; return; }
        if (lostSince < 0f) lostSince = Time.time;
        if (Time.time - lostSince < lostDelay) return;
        // Rescate excepcional de una pieza de tutorial perdida; nunca durante el agarre.
        body.position = home; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        lostSince = -1f;
    }
}

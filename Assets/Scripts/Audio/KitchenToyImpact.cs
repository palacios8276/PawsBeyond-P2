using UnityEngine;
using UnityEngine.Events;

public sealed class KitchenToyImpact : MonoBehaviour
{
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 0.4f;
    [SerializeField] private UnityEvent onFirstImpact = new UnityEvent();
    public int ImpactCount { get; private set; }

    private void OnCollisionEnter(Collision collision)
    {
        if (ImpactCount != 0 || collision.relativeVelocity.sqrMagnitude < minimumImpactSpeed * minimumImpactSpeed) return;
        if (collision.collider.GetComponentInParent<CharacterController>()) return;
        ImpactCount = 1;
        onFirstImpact.Invoke();
    }
}

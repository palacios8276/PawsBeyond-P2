using UnityEngine;

public class CharacterControllerPush : MonoBehaviour
{
    [Header("Push Settings")]
    [SerializeField] private float pushPower = 3f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // El objeto debe tener Rigidbody.
        if (body == null)
            return;

        // No empujamos objetos Kinematic.
        if (body.isKinematic)
            return;

        // Dirección horizontal en la que se mueve el jugador.
        Vector3 pushDirection = new Vector3(
            hit.moveDirection.x,
            0f,
            hit.moveDirection.z
        );

        if (pushDirection.sqrMagnitude <= 0.01f)
            return;

        pushDirection.Normalize();

        // Aplicamos la fuerza en el punto exacto de contacto.
        // Esto permite generar torque sobre objetos con bisagra.
        body.AddForceAtPosition(
            pushDirection * pushPower,
            hit.point,
            ForceMode.Impulse
        );
    }
}
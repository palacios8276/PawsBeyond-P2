using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

// Mouse movement is a frame delta; a gamepad stick is a rate.
// Handle each independently instead of applying one sensitivity to both.
public class ThirdPersonCameraInput : MonoBehaviour
{
    [SerializeField] private CinemachinePanTilt panTilt;
    [SerializeField] private float mouseDegreesPerPixel = 0.15f;
    [SerializeField] private float gamepadDegreesPerSecond = 150f;
    [SerializeField, Range(0f, 0.5f)] private float stickDeadZone = 0.18f;

    private void Awake()
    {
        if (panTilt == null) panTilt = GetComponent<CinemachinePanTilt>();
    }

    private void Update()
    {
        if (panTilt == null) return;

        Vector2 delta = Vector2.zero;

        // Only rotate from mouse when gameplay owns the cursor.
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
            delta += Mouse.current.delta.ReadValue() * mouseDegreesPerPixel;

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            if (stick.sqrMagnitude >= stickDeadZone * stickDeadZone)
                delta += stick * (gamepadDegreesPerSecond * Time.deltaTime);
        }

        if (delta.sqrMagnitude < 0.000001f) return;

        panTilt.PanAxis.Value =
            Mathf.Repeat(panTilt.PanAxis.Value + delta.x + 180f, 360f) - 180f;

        panTilt.TiltAxis.Value = Mathf.Clamp(
            panTilt.TiltAxis.Value - delta.y,
            panTilt.TiltAxis.Range.x,
            panTilt.TiltAxis.Range.y);
    }
}

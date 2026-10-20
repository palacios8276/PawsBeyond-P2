using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private float verticalVelocity;
    private float lastTurnSign = 1f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void Update()
    {
        ReadInput();
        HandleMovement();
    }

    private void ReadInput()
    {
        // Input Action principal (teclado/gamepad).
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Fallback directo del New Input System para asegurar WASD físico.
        // Esto sigue usando el New Input System y evita depender de una
        // referencia/binding serializado roto en la escena.
        if (Keyboard.current != null)
        {
            Vector2 keyboardInput = Vector2.zero;

            if (Keyboard.current.wKey.isPressed) keyboardInput.y += 1f;
            if (Keyboard.current.sKey.isPressed) keyboardInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed) keyboardInput.x += 1f;
            if (Keyboard.current.aKey.isPressed) keyboardInput.x -= 1f;

            if (keyboardInput.sqrMagnitude > 0f)
                moveInput = keyboardInput.normalized;
        }
    }

    private void HandleMovement()
    {
        if (cameraTransform == null)
            return;

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            float targetYaw = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float yaw = transform.eulerAngles.y;
            float turn = Mathf.DeltaAngle(yaw, targetYaw);
            // Keep one turn direction at the ambiguous 180-degree W/S reversal.
            if (Mathf.Abs(turn) > 179f) turn = 180f * lastTurnSign;
            else if (Mathf.Abs(turn) > 1f) lastTurnSign = Mathf.Sign(turn);
            yaw += Mathf.Clamp(turn, -rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        Vector3 finalMovement =
            moveDirection.normalized * moveSpeed * inputMagnitude;

        finalMovement.y = verticalVelocity;

        characterController.Move(finalMovement * Time.deltaTime);
    }
}


using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System;
[Serializable] public sealed class DragObjectEvent : UnityEvent<BasketDrag> { }

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Transform interactionOrigin;
    private PlayerInputActions inputActions;
    private BasketDrag heldBasket;
    [SerializeField] private DragObjectEvent onObjectGrabbed = new DragObjectEvent();
    [SerializeField] private DragObjectEvent onObjectReleased = new DragObjectEvent();
    public DragObjectEvent OnObjectGrabbed => onObjectGrabbed;
    public DragObjectEvent OnObjectReleased => onObjectReleased;
    public bool IsDragging => heldBasket && heldBasket.IsHeld;

    private void Awake() => inputActions = new PlayerInputActions();

    private void OnEnable()
    {
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Disable();
        if (heldBasket) heldBasket.Release();
        heldBasket = null;
    }

    private void OnDestroy() => inputActions?.Dispose();
    private void OnInteract(InputAction.CallbackContext context) => TryInteract();

    private void TryInteract()
    {
        if (IsDragging)
        {
            var released = heldBasket;
            heldBasket.Release();
            heldBasket = null;
            onObjectReleased.Invoke(released);
            return;
        }
        if (!interactionOrigin) return;
        if (Physics.Raycast(interactionOrigin.position, interactionOrigin.forward,
            out RaycastHit hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            var basket = hit.collider.GetComponentInParent<BasketDrag>();
            if (basket && basket.Grab(transform))
            {
                heldBasket = basket;
                onObjectGrabbed.Invoke(basket);
            }
        }
    }

}

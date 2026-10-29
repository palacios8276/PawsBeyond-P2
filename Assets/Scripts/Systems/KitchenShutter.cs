using UnityEngine;
using UnityEngine.Events;

public sealed class KitchenShutter : MonoBehaviour
{
    [SerializeField] private Rigidbody panel;
    [SerializeField] private HingeJoint hinge;
    [SerializeField] private Quaternion closedRotation = Quaternion.identity;
    [SerializeField, Min(0.1f)] private float closureTolerance = 3f;
    [SerializeField] private AudioSource closureAudio;
    [SerializeField] private WorldComicFeedback closureComic;
    [SerializeField] private UnityEvent onReleased = new UnityEvent();
    [SerializeField] private UnityEvent onClosed = new UnityEvent();
    private bool released;
    public bool IsClosed { get; private set; }
    public bool IsReleased => released;
    public int CloseCount { get; private set; }
    public UnityEvent Closed => onClosed;

    public void Release()
    {
        if (released || IsClosed || !panel || !hinge) return;
        released = true;
        panel.isKinematic = false;
        hinge.useSpring = true;
        panel.WakeUp();
        onReleased.Invoke();
    }

    private void FixedUpdate()
    {
        if (!released || IsClosed || Quaternion.Angle(panel.rotation, closedRotation) > closureTolerance) return;
        panel.linearVelocity = Vector3.zero;
        panel.angularVelocity = Vector3.zero;
        panel.isKinematic = true;
        IsClosed = true;
        CloseCount++;
        if (closureAudio && closureAudio.clip) closureAudio.Play();
        if (closureComic) closureComic.Show();
        onClosed.Invoke();
    }
}

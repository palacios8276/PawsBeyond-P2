using UnityEngine;
using UnityEngine.Events;

public sealed class BedroomShutter : MonoBehaviour
{
    [SerializeField] private BasketDrag attachedToy;
    [SerializeField] private Rigidbody shutter;
    [SerializeField] private GameObject cord;
    [SerializeField, Min(0.1f)] private float releaseDistance = 0.4f;
    [SerializeField] private float closedHeight = 1.58f;
    [SerializeField, Min(0.01f)] private float closureTolerance = 0.06f;
    [SerializeField] private UnityEvent onClosed = new UnityEvent();
    [SerializeField] private AudioSource closureAudio;
    private Vector3 toyStart;
    private bool armed, released;
    public bool IsClosed { get; private set; }

    private void Awake() => toyStart = attachedToy.transform.position;

    public void Arm()
    {
        if (armed || released || IsClosed) return;
        armed = true;
        var body = attachedToy.GetComponent<Rigidbody>();
        body.constraints = RigidbodyConstraints.FreezeAll;
        body.isKinematic = false;
    }

    private void FixedUpdate()
    {
        if (!armed || IsClosed) return;
        if (!released && attachedToy.HasBeenDragged &&
            Vector3.Distance(attachedToy.transform.position, toyStart) >= releaseDistance)
        {
            released = true;
            if (cord) cord.SetActive(false);
            attachedToy.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            shutter.isKinematic = false;
            shutter.WakeUp();
        }
        if (!released || shutter.position.y > closedHeight + closureTolerance) return;
        // La gravedad baja el panel hasta el antepecho; después queda trabado.
        shutter.linearVelocity = Vector3.zero;
        shutter.angularVelocity = Vector3.zero;
        shutter.isKinematic = true;
        IsClosed = true;
        if (closureAudio && closureAudio.clip) closureAudio.Play();
        onClosed.Invoke();
    }
}

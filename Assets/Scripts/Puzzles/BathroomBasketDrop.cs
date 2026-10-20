using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BathroomBasketDrop : MonoBehaviour
{
    [SerializeField] private Rigidbody laundryBasketRigidbody;
    [SerializeField] private Rigidbody clothingPrefab;
    [SerializeField] private Transform[] clothingSpawns;
    [SerializeField] private BoxCollider exitClearance;
    [SerializeField] private HingeJoint doorHinge;
    [SerializeField] private DoorTriggerDetector doorTrigger;
    [SerializeField] private Light interferenceLight;
    [SerializeField] private GameObject abductionBeam;
    [SerializeField] private UnityEvent onSolved = new UnityEvent();
    [SerializeField] private UnityEvent onAbductionStarted = new UnityEvent();
    [SerializeField] private UnityEvent onBasketRemoved = new UnityEvent();
    private Rigidbody[] clothes;
    private bool activated;
    private bool ready;
    private bool wasBlocked;
    private float clearTime;
    public bool IsSolved { get; private set; }
    public bool BathroomPuzzleSolved => IsSolved;
    public bool IsBasketRemoved { get; private set; }
    public int RemovedClothesCount { get; private set; }
    public bool IsActivated => activated;
    public bool IsReady => ready;
    public Rigidbody ActiveBasket => laundryBasketRigidbody;
    public Rigidbody[] Clothes => clothes;

    private void Start()
    {
        if (!laundryBasketRigidbody || !clothingPrefab || clothingSpawns == null
            || clothingSpawns.Length == 0 || !exitClearance || !doorHinge || !doorTrigger)
        {
            Debug.LogError("Bathroom puzzle: incomplete references.", this);
            enabled = false;
            return;
        }
        laundryBasketRigidbody.isKinematic = true;
        if (interferenceLight) interferenceLight.enabled = false;
        if (abductionBeam) abductionBeam.SetActive(false);
        clothes = new Rigidbody[clothingSpawns.Length];
        for (int i = 0; i < clothes.Length; i++)
        {
            clothes[i] = Instantiate(clothingPrefab, clothingSpawns[i].position, clothingSpawns[i].rotation);
            clothes[i].name = "LaundryCloth_" + (i + 1);
            clothes[i].isKinematic = true;
            clothes[i].GetComponentInChildren<Renderer>().material.color =
                Color.Lerp(new Color(0.45f, 0.55f, 0.65f), new Color(0.75f, 0.65f, 0.5f), i / (float)clothes.Length);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || activated || !other.GetComponentInParent<CharacterController>()
            || !other.transform.root.CompareTag("Player")) return;
        activated = true;
        StartCoroutine(DropLaundry());
    }

    private IEnumerator DropLaundry()
    {
        // Close first so no garment spawns inside an open door leaf.
        doorTrigger.CloseForPuzzle(true);
        if (interferenceLight) interferenceLight.enabled = true;
        while (Mathf.Abs(doorHinge.angle) > 1.5f) yield return null;
        laundryBasketRigidbody.isKinematic = false;
        for (int i = 0; i < clothes.Length; i++)
            clothes[i].isKinematic = false;
        yield return new WaitForSeconds(0.9f);
        if (interferenceLight) interferenceLight.enabled = false;
        ready = true;
        doorTrigger.CloseForPuzzle(false);
        Debug.Log("Bathroom: remove the clothes with Interact to free the door.", this);
    }

    private void FixedUpdate()
    {
        if (!ready || IsSolved) return;
        Vector3 half = Vector3.Scale(exitClearance.size * 0.5f, exitClearance.transform.lossyScale);
        bool blocked = false;
        var overlaps = Physics.OverlapBox(exitClearance.transform.TransformPoint(exitClearance.center),
            half, exitClearance.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        RemovedClothesCount = 0;
        foreach (var cloth in clothes)
        {
            bool inside = false;
            foreach (var c in overlaps) if (cloth && c.attachedRigidbody == cloth) inside = true;
            if (inside) blocked = true;
            if (cloth && !inside && cloth.GetComponent<BasketDrag>().HasBeenDragged) RemovedClothesCount++;
        }
        if (blocked) wasBlocked = true;
        clearTime = !blocked && wasBlocked && RemovedClothesCount == clothes.Length
            ? clearTime + Time.fixedDeltaTime : 0f;
        if (clearTime < 0.4f) return;
        IsSolved = true;
        onSolved.Invoke();
        StartCoroutine(AbductBasket());
        Debug.Log("Bathroom puzzle solved: clothes removed, door free.", this);
    }

    private IEnumerator AbductBasket()
    {
        // This event only follows the player's solution; it cannot remove the clothes.
        onAbductionStarted.Invoke();
        laundryBasketRigidbody.linearVelocity = Vector3.zero;
        laundryBasketRigidbody.angularVelocity = Vector3.zero;
        laundryBasketRigidbody.isKinematic = true;
        laundryBasketRigidbody.detectCollisions = false;
        if (interferenceLight) interferenceLight.enabled = true;
        if (abductionBeam) abductionBeam.SetActive(true);
        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            laundryBasketRigidbody.MovePosition(laundryBasketRigidbody.position + Vector3.up * (2f * Time.fixedDeltaTime));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        laundryBasketRigidbody.gameObject.SetActive(false);
        if (interferenceLight) interferenceLight.enabled = false;
        if (abductionBeam) abductionBeam.SetActive(false);
        IsBasketRemoved = true;
        onBasketRemoved.Invoke();
    }
}

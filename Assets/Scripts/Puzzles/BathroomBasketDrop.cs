using System.Collections;
using System.Collections.Generic;
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
    [Header("Accidente: un ladrido aceptado dentro del baño")]
    [SerializeField] private AbductionBeamController beamController;
    [SerializeField] private PlayerBark playerBark;
    [SerializeField] private BathroomAccident accident;
    [SerializeField] private BathroomImpactFeedback basketImpact;
    [SerializeField] private AudioSource solvedAudio;
    private BoxCollider playerZone;

    private void OnEnable()
    {
        playerZone = GetComponent<BoxCollider>();
        if (beamController) beamController.InterferenceAccepted.AddListener(TryActivate);
    }

    private void OnDisable()
    {
        if (beamController) beamController.InterferenceAccepted.RemoveListener(TryActivate);
        playersInside.Clear();
    }

    private readonly HashSet<Collider> playersInside = new HashSet<Collider>();
    private bool ownersArrived;
    private bool initialized;
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
        initialized = true;
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

    // La llegada narrativa y la presencia del perro pueden ocurrir en cualquier orden.
    public void NotifyOwnersArrived()
    {
        ownersArrived = true;
    }

    private void OnTriggerEnter(Collider other) => TrackPlayer(other);
    private void OnTriggerStay(Collider other) => TrackPlayer(other);
    private void OnTriggerExit(Collider other) => playersInside.Remove(other);

    private void TrackPlayer(Collider other)
    {
        if (!other.GetComponentInParent<CharacterController>() || !other.transform.root.CompareTag("Player")) return;
        playersInside.Add(other);
    }

    private void TryActivate()
    {
        if (!enabled || activated || !initialized || !ownersArrived || !beamController
            || !beamController.IsBeamActive || !playerBark || !playerZone) return;
        playersInside.RemoveWhere(c => !c || !c.enabled || !c.gameObject.activeInHierarchy);
        if (playersInside.Count == 0) return;
        // Revalidar la posición evita aceptar un trigger obsoleto al salir del baño.
        Vector3 local = playerZone.transform.InverseTransformPoint(playerBark.Origin.position) - playerZone.center;
        Vector3 half = playerZone.size * 0.5f;
        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z) return;
        activated = true;
        StartCoroutine(DropLaundry());
    }

    private IEnumerator DropLaundry()
    {
        // Close first so no garment spawns inside an open door leaf.
        doorTrigger.CloseForPuzzle(true);
        if (interferenceLight) interferenceLight.enabled = true;
        while (Mathf.Abs(doorHinge.angle) > 1.5f) yield return null;
        if (accident) yield return accident.ReactAndTip();
        doorTrigger.LockForPuzzle();
        if (basketImpact) basketImpact.Arm();
        laundryBasketRigidbody.isKinematic = false;
        for (int i = 0; i < clothes.Length; i++)
            clothes[i].isKinematic = false;
        yield return new WaitForSeconds(0.9f);
        if (interferenceLight) interferenceLight.enabled = false;
        ready = true;

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
        doorTrigger.CloseForPuzzle(false);
        if (solvedAudio) solvedAudio.Play();
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

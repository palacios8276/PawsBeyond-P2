using UnityEngine;
using UnityEngine.Events;
using TMPro;

public enum BedroomTutorialStep { Move, Look, FirstToy, DropFirst, SecondToy, DropSecond, Bark, ConsecutiveBarks, Abduction, ThirdToy, Shutter, Complete }

public sealed class BedroomTutorial : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform viewingCamera;
    [SerializeField] private PlayerInteraction interaction;
    [SerializeField] private PlayerBark bark;
    [SerializeField] private BasketDrag firstToy, secondToy, thirdToy;
    [SerializeField] private Renderer firstHighlight, secondHighlight, thirdHighlight;
    [SerializeField] private BedroomShutter shutter;
    [SerializeField] private OwnersJourney journey;
    [SerializeField] private Transform abductionTarget;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private OwnerFloat[] owners;
    [Header("Ayudas y ritmo")]
    [SerializeField] private TMP_Text hint;
    [SerializeField] private GameplayGuidance guidance;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private Vector3 roomCenter = new Vector3(-18f, 0f, 0f);
    [SerializeField] private Vector2 roomHalfSize = new Vector2(6f, 4f);
    [SerializeField, Min(1f)] private float hintSeconds = 8f;
    [SerializeField, Min(0.1f)] private float moveDistance = 0.7f;
    [SerializeField, Min(1f)] private float lookDegrees = 12f;
    [SerializeField, Min(0.1f)] private float abductionSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float ownerReactionRange = 5f;
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.15f);
    [SerializeField] private UnityEvent onAbductionStarted = new UnityEvent();
    [SerializeField] private UnityEvent onBedroomCompleted = new UnityEvent();
    [SerializeField] private BedroomTutorialStep step;
    private Vector3 moveStart;
    private Quaternion lookStart;
    private float hintUntil;
    private bool wasInRoom;
    private int lastBark = -1;
    private MaterialPropertyBlock originalHighlight, highlightBlock;
    private Renderer highlighted;
    public BedroomTutorialStep Step => step;

    private void Start()
    {
        moveStart = player.position;
        lookStart = viewingCamera.rotation;
        journey.SetNarrativeHold(true);
        ChangeStep(BedroomTutorialStep.Move);
    }

    private void OnEnable()
    {
        if (interaction) { interaction.OnObjectGrabbed.AddListener(OnGrabbed); interaction.OnObjectReleased.AddListener(OnReleased); }
    }

    private void OnDisable()
    {
        if (interaction) { interaction.OnObjectGrabbed.RemoveListener(OnGrabbed); interaction.OnObjectReleased.RemoveListener(OnReleased); }
        RestoreHighlight();
        if (!guidance && hintPanel) hintPanel.SetActive(false);
    }

    private bool InRoom() => Mathf.Abs(player.position.x - roomCenter.x) <= roomHalfSize.x &&
        Mathf.Abs(player.position.z - roomCenter.z) <= roomHalfSize.y;

    private void Update()
    {
        bool inRoom = InRoom();
        if (inRoom && !wasInRoom) hintUntil = Time.time + hintSeconds;
        wasInRoom = inRoom;
        if (!guidance && hintPanel) hintPanel.SetActive(inRoom && Time.time < hintUntil && step != BedroomTutorialStep.Complete);
        if (step == BedroomTutorialStep.Abduction)
        {
            journey.transform.position = Vector3.MoveTowards(journey.transform.position, abductionTarget.position, abductionSpeed * Time.deltaTime);
            if (Vector3.Distance(journey.transform.position, abductionTarget.position) < 0.02f)
            { shutter.Arm(); ChangeStep(BedroomTutorialStep.ThirdToy); }
        }
        if (!inRoom) return;
        if (step == BedroomTutorialStep.DropFirst && !firstToy.IsHeld) ChangeStep(BedroomTutorialStep.SecondToy);
        else if (step == BedroomTutorialStep.DropSecond && !secondToy.IsHeld) ChangeStep(BedroomTutorialStep.Bark);
        if (step == BedroomTutorialStep.Move && Vector3.ProjectOnPlane(player.position - moveStart, Vector3.up).magnitude >= moveDistance)
        { lookStart = viewingCamera.rotation; ChangeStep(BedroomTutorialStep.Look); }
        else if (step == BedroomTutorialStep.Look && Quaternion.Angle(lookStart, viewingCamera.rotation) >= lookDegrees)
            ChangeStep(BedroomTutorialStep.FirstToy);
        else if (step == BedroomTutorialStep.ThirdToy && thirdToy.IsHeld)
            ChangeStep(BedroomTutorialStep.Shutter);
    }

    private void OnGrabbed(BasketDrag toy)
    {
        if (!InRoom()) return;
        if (step == BedroomTutorialStep.FirstToy && toy == firstToy) ChangeStep(BedroomTutorialStep.DropFirst);
        else if (step == BedroomTutorialStep.SecondToy && toy == secondToy) ChangeStep(BedroomTutorialStep.DropSecond);
    }

    private void OnReleased(BasketDrag toy)
    {
        if (!InRoom()) return;
        if (step == BedroomTutorialStep.DropFirst && toy == firstToy) ChangeStep(BedroomTutorialStep.SecondToy);
        else if (step == BedroomTutorialStep.DropSecond && toy == secondToy) ChangeStep(BedroomTutorialStep.Bark);
    }

    public void OnBark()
    {
        if (!isActiveAndEnabled || !InRoom() || bark.BarkCount == lastBark) return;
        lastBark = bark.BarkCount;
        if (step == BedroomTutorialStep.Bark) ChangeStep(BedroomTutorialStep.ConsecutiveBarks);
        else if (step == BedroomTutorialStep.ConsecutiveBarks && bark.IsLastBarkReinforced &&
            Vector3.Distance(player.position, journey.transform.position) <= ownerReactionRange)
        {
            foreach (var owner in owners) if (owner) owner.ReactToBarks();
            beam.SetBeamActive(true);
            journey.BeginJourney();
            ChangeStep(BedroomTutorialStep.Abduction);
            onAbductionStarted.Invoke();
        }
    }

    public void OnShutterClosed()
    {
        if (!shutter.IsClosed || (step != BedroomTutorialStep.Shutter && step != BedroomTutorialStep.ThirdToy)) return;
        ChangeStep(BedroomTutorialStep.Complete);
        progression.CompleteBedroom();
        journey.SetNarrativeHold(false);
        onBedroomCompleted.Invoke();
    }

    private void ChangeStep(BedroomTutorialStep next)
    {
        RestoreHighlight();
        step = next;
        hintUntil = Time.time + hintSeconds;
        if (guidance) guidance.PresentTutorialStep(next);
        else if (hint) hint.text = Message(next);
        Renderer target = next == BedroomTutorialStep.FirstToy || next == BedroomTutorialStep.DropFirst ? firstHighlight
            : next == BedroomTutorialStep.SecondToy || next == BedroomTutorialStep.DropSecond ? secondHighlight
            : next == BedroomTutorialStep.ThirdToy || next == BedroomTutorialStep.Shutter ? thirdHighlight : null;
        if (!target) return;
        highlighted = target;
        originalHighlight = new MaterialPropertyBlock(); target.GetPropertyBlock(originalHighlight);
        highlightBlock = new MaterialPropertyBlock(); target.GetPropertyBlock(highlightBlock);
        highlightBlock.SetColor("_BaseColor", highlightColor); target.SetPropertyBlock(highlightBlock);
    }

    private void RestoreHighlight()
    {
        if (highlighted && originalHighlight != null) highlighted.SetPropertyBlock(originalHighlight);
        highlighted = null;
    }

    private static string Message(BedroomTutorialStep value)
    {
        switch (value)
        {
            case BedroomTutorialStep.Move: return "Movete con WASD / stick izquierdo";
            case BedroomTutorialStep.Look: return "Mirá alrededor: mouse / stick derecho";
            case BedroomTutorialStep.FirstToy: return "Acercate al juguete iluminado y agarralo con E / cruz";
            case BedroomTutorialStep.DropFirst: return "Volvé a pulsar E / cruz para soltarlo";
            case BedroomTutorialStep.SecondToy: return "Probá agarrar el otro juguete iluminado";
            case BedroomTutorialStep.DropSecond: return "Soltá el juguete con E / cruz";
            case BedroomTutorialStep.Bark: return "Probá ladrar: B / cuadrado";
            case BedroomTutorialStep.ConsecutiveBarks: return "Cerca de los dueños, probá dos ladridos seguidos";
            case BedroomTutorialStep.Abduction: return "¡Algo está llevando a tus dueños hacia la ventana!";
            case BedroomTutorialStep.ThirdToy: return "El juguete junto a la silla está trabado. Agarralo y tirá";
            case BedroomTutorialStep.Shutter: return "Alejate de la silla llevando el juguete para soltar el amarre";
            default: return "";
        }
    }
}

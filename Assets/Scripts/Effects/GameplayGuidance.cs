using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// Presentación local del Canvas existente: recibe hechos del juego, nunca lo hace avanzar.
public sealed class GameplayGuidance : MonoBehaviour
{
    private enum ControlHint { None, Move, Look, Interact, Bark }

    [Header("Panel existente y jerarquía de lectura")]
    [SerializeField] private CanvasGroup noticeGroup;
    [SerializeField] private TMP_Text title, detail, objective;
    [SerializeField] private RectTransform noticeRoot;
    [Header("Referencias de contexto")]
    [SerializeField] private BedroomTutorial tutorial;
    [SerializeField] private LivingRoomEpisode living;
    [SerializeField] private EpisodeProgression progression;
    [SerializeField] private AbductionBeamController beam;
    [SerializeField] private BathroomBasketDrop bathroom;
    [SerializeField] private ScentTrailPath humanScentTrail;
    [SerializeField] private ContextualCameraDirector contextualCamera;
    [Header("Lectura y transición")]
    [SerializeField, FormerlySerializedAs("minimumReadSeconds"), Min(1f)] private float initialDuration = 3f;
    [SerializeField, Min(1f)] private float inactivitySeconds = 2.5f;
    [SerializeField, Min(1f)] private float reminderDuration = 2.5f;
    [SerializeField, Range(0.1f, 0.5f)] private float fadeSeconds = 0.2f;
    [SerializeField, Min(1f)] private float reminderRepeatInterval = 8f;
    [SerializeField, Range(0.05f, 0.9f)] private float movementThreshold = 0.25f;
    [SerializeField, Min(0.1f)] private float pointerDeltaThreshold = 0.8f;

    private sealed class Notice
    {
        public string id, heading, body;
        public int context, priority;
        public ControlHint controlHint;
        public bool remindable;
    }

    private readonly List<Notice> pending = new List<Notice>();
    private readonly HashSet<string> shown = new HashSet<string>();
    private Coroutine presentation;
    private Notice currentNotice, reminder;
    private int context;
    private bool closing, presentingReminder;
    private bool humanScentNoticeRequested;
    private float lastActivityAt, nextReminderAt;
    private Coroutine humanScentRoutine;

    public string CurrentNoticeId { get; private set; }
    public string CurrentObjective => reminder != null ? reminder.heading : "";
    public string ActiveDeviceLabel => "Keyboard+PlayStation";
    public int PresentedCount { get; private set; }

    private void Awake()
    {
        if (noticeGroup)
        {
            noticeGroup.alpha = 0f;
            noticeGroup.blocksRaycasts = false;
            noticeGroup.interactable = false;
        }
        if (objective)
        {
            objective.text = "";
            objective.gameObject.SetActive(false);
        }
        lastActivityAt = Time.unscaledTime;
    }

    private void OnEnable()
    {
        if (beam) beam.InterferenceAccepted.AddListener(OnInterferenceAccepted);
        if (humanScentTrail) humanScentTrail.FirstHumanTrailVisible += OnFirstHumanTrailVisible;
        if (bathroom)
        {
            bathroom.AccidentStarted += OnBathroomAccident;
            bathroom.ClothesReady += OnClothesReady;
        }
    }

    private void Update()
    {
        bool meaningfulActivity = HasMeaningfulActivity();
        if (meaningfulActivity)
        {
            lastActivityAt = Time.unscaledTime;
            nextReminderAt = Mathf.Max(nextReminderAt, lastActivityAt + inactivitySeconds);
            if (presentingReminder) StopPresentation();
        }

        if (closing || presentation != null || reminder == null || pending.Count > 0) return;
        float now = Time.unscaledTime;
        if (now - lastActivityAt < inactivitySeconds || now < nextReminderAt) return;
        nextReminderAt = now + reminderRepeatInterval;
        presentation = StartCoroutine(PresentReminder(reminder));
    }

    private void ChangeContext()
    {
        if (closing) return;
        context++;
        pending.Clear();
        reminder = null;
        nextReminderAt = Time.unscaledTime + initialDuration + inactivitySeconds;
    }

    private void Tell(string id, string heading, string body, ControlHint hint = ControlHint.None,
        int priority = 2, bool remindable = true)
    {
        string noticeKey = NoticeKey(id, context);
        if (closing || !isActiveAndEnabled || !noticeGroup || !title || !detail || shown.Contains(noticeKey)) return;
        if (pending.Exists(x => x.id == id)) return;
        var notice = new Notice
        {
            id = id,
            heading = heading,
            body = body,
            context = context,
            priority = priority,
            controlHint = hint,
            remindable = remindable
        };
        pending.Add(notice);
        if (remindable)
        {
            reminder = notice;
            nextReminderAt = Time.unscaledTime + initialDuration + inactivitySeconds;
        }
        if (presentation == null) presentation = StartCoroutine(PresentQueued());
    }

    private IEnumerator PresentQueued()
    {
        while (pending.Count > 0)
        {
            pending.Sort((a, b) => b.priority.CompareTo(a.priority));
            Notice next = pending[0];
            pending.RemoveAt(0);
            string noticeKey = NoticeKey(next.id, next.context);
            if (next.context != context || shown.Contains(noticeKey)) continue;
            shown.Add(noticeKey);
            PresentedCount++;
            yield return Show(next, initialDuration, false);
        }
        presentation = null;
    }

    private IEnumerator PresentReminder(Notice notice)
    {
        if (notice.context == context)
        {
            PresentedCount++;
            yield return Show(notice, reminderDuration, true);
        }
        presentation = null;
    }

    private IEnumerator Show(Notice notice, float duration, bool isReminder)
    {
        currentNotice = notice;
        presentingReminder = isReminder;
        CurrentNoticeId = notice.id;
        RefreshVisibleText();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (notice.context != context || (!isReminder && pending.Count > 0)) break;
            noticeGroup.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
            if (noticeRoot)
                noticeRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f,
                    Mathf.SmoothStep(0f, 1f, elapsed / fadeSeconds));
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
        {
            noticeGroup.alpha = 1f - t / fadeSeconds;
            yield return null;
        }
        HideNotice();
    }

    private void StopPresentation()
    {
        if (presentation != null) StopCoroutine(presentation);
        presentation = null;
        HideNotice();
    }

    private void HideNotice()
    {
        if (noticeGroup) noticeGroup.alpha = 0f;
        currentNotice = null;
        presentingReminder = false;
        CurrentNoticeId = null;
    }

    private void RefreshVisibleText()
    {
        if (currentNotice == null) return;
        title.text = currentNotice.heading;
        detail.text = currentNotice.body + ControlSuffix(currentNotice.controlHint);
    }

    private string ControlSuffix(ControlHint hint)
    {
        if (hint == ControlHint.None) return "";
        if (hint == ControlHint.Move) return "\nWASD (teclado) / stick izquierdo (PlayStation)";
        if (hint == ControlHint.Look) return "\nMouse (teclado) / stick derecho (PlayStation)";
        // LiberationSans no contiene U+2715. La X ASCII identifica el mismo botón sin warnings TMP.
        if (hint == ControlHint.Interact) return "\nE (teclado) / X (PlayStation)";
        return "\nB (teclado) / □ (PlayStation)";
    }

    private bool HasMeaningfulActivity()
    {
        if (KeyboardMouseWasUsed()) return true;
        foreach (Gamepad gamepad in Gamepad.all)
            if (GamepadWasUsed(gamepad)) return true;
        return false;
    }

    private bool KeyboardMouseWasUsed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed ||
            keyboard.sKey.isPressed || keyboard.dKey.isPressed || keyboard.eKey.wasPressedThisFrame ||
            keyboard.bKey.wasPressedThisFrame)) return true;
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.delta.ReadValue().sqrMagnitude >
            pointerDeltaThreshold * pointerDeltaThreshold;
    }

    private bool GamepadWasUsed(Gamepad gamepad)
    {
        if (gamepad == null) return false;
        float thresholdSquared = movementThreshold * movementThreshold;
        return gamepad.leftStick.ReadValue().sqrMagnitude > thresholdSquared ||
            gamepad.rightStick.ReadValue().sqrMagnitude > thresholdSquared ||
            gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame;
    }

    private static string NoticeKey(string id, int noticeContext) => id + "@" + noticeContext;

    public void PresentTutorialStep(BedroomTutorialStep step)
    {
        ChangeContext();
        switch (step)
        {
            case BedroomTutorialStep.Move:
                Tell("move", "¡EXPLORÁ EL DORMITORIO!", "Recorré el cuarto y acercate a los objetos.", ControlHint.Move);
                break;
            case BedroomTutorialStep.Look:
                Tell("look", "¡MIRÁ A TU ALREDEDOR!", "Orientá la cámara para descubrir con qué podés interactuar.", ControlHint.Look);
                break;
            case BedroomTutorialStep.FirstToy:
            case BedroomTutorialStep.SecondToy:
                Tell("toy", "¡AGARRÁ EL JUGUETE!", "Acercate al juguete iluminado y tomalo.", ControlHint.Interact);
                break;
            case BedroomTutorialStep.DropFirst:
            case BedroomTutorialStep.DropSecond:
                Tell("drop", "¡SOLTÁ EL JUGUETE!", "Llevalo con vos y volvé a pulsar para soltarlo.", ControlHint.Interact);
                break;
            case BedroomTutorialStep.Bark:
                Tell("bark", "¡LADRÁ!", "Probá tu ladrido y soltá el botón entre pulsaciones.", ControlHint.Bark);
                break;
            case BedroomTutorialStep.ConsecutiveBarks:
                Tell("combo", "¡LADRÁ DOS VECES!", "Cerca de tus dueños, dos ladridos seguidos refuerzan el segundo.", ControlHint.Bark);
                break;
            case BedroomTutorialStep.Abduction:
                Tell("beam", "¡EL RAYO REACCIONA A TUS LADRIDOS!", "Acercate a la energía, mirá hacia ella y ladrá.", ControlHint.Bark);
                break;
            case BedroomTutorialStep.ThirdToy:
            case BedroomTutorialStep.Shutter:
                Tell("shutter", "¡TIRÁ DEL JUGUETE!", "Agarrá el juguete junto a la silla y alejate para tensar el amarre.", ControlHint.Interact);
                break;
            case BedroomTutorialStep.Complete:
                Tell("shutter-closed", "¡SEGUÍ A TUS DUEÑOS!", "Cortaste esta entrada del rayo, pero todavía siguen en peligro.", priority: 1);
                break;
        }
    }

    public void OnWindowAttempt()
    {
        ChangeContext();
        Tell("living-beam", "¡LADRÁ CERCA DEL RAYO!", "Acercate, mirá hacia la energía e interrumpila.", ControlHint.Bark);
    }

    private void OnInterferenceAccepted()
    {
        if (!beam || !beam.IsBeamActive || !progression) return;
        if (progression.CurrentEpisode == NarrativeEpisode.LivingRoom && living && living.IsWaitingForBarks)
            Tell("living-hit", "¡EL RAYO ESTÁ FALLANDO!", "Tu ladrido produjo interferencia. Seguí interrumpiéndolo.",
                priority: 1, remindable: false);
        else if (progression.CurrentEpisode == NarrativeEpisode.Bedroom && tutorial &&
            tutorial.Step >= BedroomTutorialStep.Abduction)
            Tell("bedroom-hit", "¡EL RAYO REACCIONÓ!", "La alteración del haz confirma que tu ladrido lo alcanzó.",
                priority: 1, remindable: false);
    }

    public void OnWindowShifted()
    {
        ChangeContext();
        Tell("shift", "¡SEGUÍ A TUS DUEÑOS!", "La nave cambió de entrada. Acompañá su recorrido.", priority: 1);
    }

    public void OnBathroomArrived()
    {
        ChangeContext();
        Tell("bathroom-bark", "¡LADRÁ CERCA DE TUS DUEÑOS!", "Entrá al baño, acercate y hacé que te escuchen.", ControlHint.Bark);
    }

    private void OnBathroomAccident()
    {
        ChangeContext();
        Tell("accident", "¡ALGO BLOQUEÓ LA SALIDA!", "Tu ladrido los sobresaltó. Mirá qué cayó cerca de la puerta.",
            priority: 1);
    }

    private void OnClothesReady()
    {
        ChangeContext();
        Tell("clothes", "PUERTA BLOQUEADA", "Sacá las prendas de la cesta para destrabar la puerta.",
            ControlHint.Interact);
    }

    public void OnBathroomSolved()
    {
        ChangeContext();
        Tell("bathroom-clear", "¡SEGUÍ A TUS DUEÑOS!", "La puerta quedó libre. Salí del baño y continuá.", priority: 1);
    }

    public void OnKitchenStarted()
    {
        ChangeContext();
        Tell("alien-a", "¡AHUYENTÁ AL ALIENÍGENA!", "Acercate, miralo y ladrá hasta obligarlo a retirarse.", ControlHint.Bark);
    }

    public void OnAlienAToyThrown()
    {
        Tell("alien-a-toy", "¿ME ESTÁ TIRANDO UN JUGUETE?", "¡Ahora no, estoy ocupado!",
            priority: 1, remindable: false);
    }

    public void OnAlienHidden()
    {
        ChangeContext();
        Tell("hidden", "¡BUSCÁ AL ALIENÍGENA!", "Explorá y prestá atención a las pistas del entorno.");
    }

    public void OnAlienDiscovered()
    {
        ChangeContext();
        Tell("discovered", "¡VOLVÉ A LADRARLE!", "Lo encontraste. Seguí interrumpiéndolo para hacerlo salir.",
            ControlHint.Bark, priority: 1);
    }

    public void OnRepairStarted()
    {
        ChangeContext();
        Tell("repair", "¿QUÉ ESTARÁ DICIENDO?", "¡Mejor le ladro!",
            ControlHint.Bark);
    }

    public void OnRepairInterrupted()
    {
        ChangeContext();
        Tell("repair-stopped", "¡SEGUÍ A TUS DUEÑOS!", "Detuviste la reparación. Acompañalos cuando retomen el recorrido.",
            priority: 1);
    }

    public void OnKitchenCompleted()
    {
        ChangeContext();
        Tell("return", "SEGUÍ A TUS PADRES", "", priority: 1);
    }

    private void OnFirstHumanTrailVisible()
    {
        if (closing || humanScentNoticeRequested) return;
        humanScentNoticeRequested = true;
        humanScentRoutine = StartCoroutine(PresentHumanScentTutorialWhenSafe());
    }

    private IEnumerator PresentHumanScentTutorialWhenSafe()
    {
        while (!closing && ((contextualCamera && contextualCamera.IsPlaying) ||
            (tutorial && tutorial.Step != BedroomTutorialStep.Complete) || presentation != null ||
            pending.Count > 0))
            yield return null;

        if (!closing)
            Tell("human-scent", "¿ESOS CUADROS NARANJAS SON UN OLOR?", "¡Voy a seguirlos!", priority: 1,
                remindable: false);
        humanScentRoutine = null;
    }

    public void OnClosingBarkRequested()
    {
        ChangeContext();
        Tell("closing-bark", "¡LADRÁ UNA VEZ MÁS!", "B (teclado) / □ (PlayStation)",
            priority: 3);
    }

    public void OnClosingBarkCancelled()
    {
        ChangeContext();
    }

    public void OnClosingStarted()
    {
        closing = true;
        StopAllCoroutines();
        presentation = null;
        pending.Clear();
        reminder = null;
        HideNotice();
    }

    private void OnDisable()
    {
        if (beam) beam.InterferenceAccepted.RemoveListener(OnInterferenceAccepted);
        if (humanScentTrail) humanScentTrail.FirstHumanTrailVisible -= OnFirstHumanTrailVisible;
        if (bathroom)
        {
            bathroom.AccidentStarted -= OnBathroomAccident;
            bathroom.ClothesReady -= OnClothesReady;
        }
        StopAllCoroutines();
        presentation = null;
        humanScentRoutine = null;
        pending.Clear();
        reminder = null;
        HideNotice();
    }
}

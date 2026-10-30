using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Presentación local del Canvas existente: recibe hechos del juego, nunca lo hace avanzar.
public sealed class GameplayGuidance : MonoBehaviour
{
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
    [Header("Lectura y transición")]
    [SerializeField, Min(1f)] private float minimumReadSeconds = 3f;
    [SerializeField, Min(1f)] private float noticeSeconds = 7f;
    [SerializeField, Range(0.1f, 0.5f)] private float fadeSeconds = 0.2f;
    private sealed class Notice
    {
        public string id, heading, body;
        public int context, priority;
    }
    private readonly List<Notice> pending = new List<Notice>();
    private readonly HashSet<string> shown = new HashSet<string>();
    private Coroutine presentation;
    private int context;
    private bool closing;
    public string CurrentNoticeId { get; private set; }
    public string CurrentObjective => objective ? objective.text : "";
    public int PresentedCount { get; private set; }

    private void Awake()
    {
        if (noticeGroup) { noticeGroup.alpha = 0f; noticeGroup.blocksRaycasts = false; noticeGroup.interactable = false; }
    }

    private void OnEnable()
    {
        if (beam) beam.InterferenceAccepted.AddListener(OnInterferenceAccepted);
        if (bathroom) { bathroom.AccidentStarted += OnBathroomAccident; bathroom.ClothesReady += OnClothesReady; }
    }

    private void SetObjective(string text)
    {
        if (!objective || closing || objective.text == text) return;
        objective.text = text;
        context++;
        pending.Clear(); // Una instrucción pendiente de una etapa anterior ya no es pertinente.
    }

    private void Tell(string id, string heading, string body, int priority = 2)
    {
        if (closing || !isActiveAndEnabled || !noticeGroup || !title || !detail || shown.Contains(id)) return;
        if (pending.Exists(x => x.id == id)) return;
        pending.Add(new Notice { id = id, heading = heading, body = body, context = context, priority = priority });
        if (presentation == null) presentation = StartCoroutine(Present());
    }

    private IEnumerator Present()
    {
        while (pending.Count > 0)
        {
            pending.Sort((a, b) => b.priority.CompareTo(a.priority));
            Notice next = pending[0]; pending.RemoveAt(0);
            if (next.context != context || shown.Contains(next.id)) continue;
            shown.Add(next.id);
            PresentedCount++;
            CurrentNoticeId = next.id;
            title.text = next.heading; detail.text = next.body;
            float elapsed = 0f;
            float hold = Mathf.Max(minimumReadSeconds, noticeSeconds);
            while (elapsed < hold)
            {
                // Se conserva un mínimo de lectura; luego se retira información que quedó obsoleta.
                if (elapsed >= minimumReadSeconds && (next.context != context || pending.Count > 0)) break;
                noticeGroup.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
                noticeRoot.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, Mathf.SmoothStep(0f, 1f, elapsed / fadeSeconds));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            { noticeGroup.alpha = 1f - t / fadeSeconds; yield return null; }
            noticeGroup.alpha = 0f;
            CurrentNoticeId = null;
        }
        presentation = null;
    }

    public void PresentTutorialStep(BedroomTutorialStep step)
    {
        switch (step)
        {
            case BedroomTutorialStep.Move:
                SetObjective("OBJETIVO: EXPLORÁ EL DORMITORIO · WASD / stick izquierdo");
                Tell("move", "¡PATAS EN MARCHA!", "Movete con WASD / stick izquierdo para explorar y acercarte a los objetos."); break;
            case BedroomTutorialStep.Look:
                SetObjective("OBJETIVO: MIRÁ A TU ALREDEDOR · mouse / stick derecho");
                Tell("look", "¡MIRÁ A TU ALREDEDOR!", "Mové el mouse / stick derecho para orientar la cámara y descubrir con qué interactuar."); break;
            case BedroomTutorialStep.FirstToy:
            case BedroomTutorialStep.SecondToy:
                SetObjective("OBJETIVO: AGARRÁ EL JUGUETE ILUMINADO · E / cruz (A en Xbox)");
                Tell("toy", "¡AGARRALO CON LA BOCA!", "Acercate y mirá el juguete iluminado: E / cruz (A en Xbox) lo agarra para llevarlo."); break;
            case BedroomTutorialStep.DropFirst:
            case BedroomTutorialStep.DropSecond:
                SetObjective("OBJETIVO: SOLTÁ EL JUGUETE · E / cruz (A en Xbox)");
                Tell("drop", "¡LLEVÁ Y SOLTÁ!", "Movete con el objeto en la boca. Otra pulsación de E / cruz (A en Xbox) lo suelta."); break;
            case BedroomTutorialStep.Bark:
                SetObjective("OBJETIVO: PROBÁ TU LADRIDO · B / cuadrado (X en Xbox)");
                Tell("bark", "¡HACETE ESCUCHAR!", "Pulsá B / cuadrado (X en Xbox) para ladrar. Soltá el botón entre pulsaciones."); break;
            case BedroomTutorialStep.ConsecutiveBarks:
                SetObjective("OBJETIVO: LADRÁ DOS VECES SEGUIDAS CERCA DE TUS DUEÑOS");
                Tell("combo", "¡DOS LADRIDOS TIENEN MÁS FUERZA!", "Cerca de tus dueños, pulsá dos veces B / cuadrado (X en Xbox) para reforzar el segundo ladrido."); break;
            case BedroomTutorialStep.Abduction:
                SetObjective("OBJETIVO: OBSERVÁ LA ENERGÍA Y SEGUÍ A TUS DUEÑOS");
                Tell("beam", "¡TUS LADRIDOS ALTERAN EL RAYO!", "Ladrá cerca de la energía y mirá hacia ella: observá cómo reacciona la tecnología."); break;
            case BedroomTutorialStep.ThirdToy:
            case BedroomTutorialStep.Shutter:
                SetObjective("OBJETIVO: TIRÁ DEL JUGUETE JUNTO A LA SILLA");
                Tell("shutter", "¡ESE JUGUETE ESTÁ AMARRADO!", "Agarralo con E / cruz (A en Xbox) y alejate: tirar del amarre puede mover lo que sostiene."); break;
            case BedroomTutorialStep.Complete:
                SetObjective("OBJETIVO: SEGUÍ A TUS DUEÑOS HACIA LA SALA");
                Tell("shutter-closed", "¡CORTASTE LA ENTRADA DEL RAYO!", "La persiana cerrada interrumpió esta conexión. Tus dueños siguen en peligro: seguí su recorrido.", 1); break;
        }
    }

    public void OnWindowAttempt()
    {
        SetObjective("OBJETIVO: INTERRUMPÍ EL RAYO · B / cuadrado (X en Xbox)");
        Tell("living-beam", "¡TUS LADRIDOS ALTERAN EL RAYO!", "Acercate, mirá hacia la energía y ladrá. Dos pulsaciones seguidas refuerzan la interferencia.");
    }

    private void OnInterferenceAccepted()
    {
        if (!beam || !beam.IsBeamActive || !progression) return;
        if (progression.CurrentEpisode == NarrativeEpisode.LivingRoom && living && living.IsWaitingForBarks)
            Tell("living-hit", "¡EL RAYO ESTÁ FALLANDO!", "Tu ladrido produjo interferencia: observá su color, pulso y deformación. Seguí interrumpiéndolo.", 1);
        else if (progression.CurrentEpisode == NarrativeEpisode.Bedroom && tutorial && tutorial.Step >= BedroomTutorialStep.Abduction)
            Tell("bedroom-hit", "¡EL RAYO REACCIONÓ!", "Tu ladrido alteró la energía. La reacción del haz confirma que estás lo bastante cerca y bien orientado.", 1);
    }

    public void OnWindowShifted()
    {
        SetObjective("OBJETIVO: SEGUÍ EL RECORRIDO DE TUS DUEÑOS");
        Tell("shift", "¡LA NAVE CAMBIA DE OBJETIVO!", "La interferencia obligó a buscar otra entrada. Seguí a tus dueños.", 1);
    }

    public void OnBathroomArrived()
    {
        SetObjective("OBJETIVO: ENTRÁ AL BAÑO Y LADRÁ CERCA DE TUS DUEÑOS");
        Tell("bathroom-bark", "¡HACETE ESCUCHAR EN EL BAÑO!", "Tus dueños esperan adentro. Acercate, mirá hacia ellos y ladrá para provocar una reacción.");
    }

    private void OnBathroomAccident()
    {
        SetObjective("OBJETIVO: OBSERVÁ QUÉ BLOQUEÓ LA SALIDA");
        Tell("accident", "¡TU LADRIDO LOS SOBRESALTÓ!", "La reacción de tus dueños sacudió el soporte del cesto. Prestá atención a lo que cae.", 1);
    }

    private void OnClothesReady()
    {
        SetObjective("OBJETIVO: RETIRÁ LAS TRES PRENDAS DE LA PUERTA");
        Tell("clothes", "¡DESPEJÁ LA SALIDA!", "Mirá cada prenda y agarrala con E / cruz (A en Xbox). Llevá las tres lejos de la puerta y soltálas con el mismo botón.");
    }

    public void OnBathroomSolved()
    {
        SetObjective("OBJETIVO: SALÍ DEL BAÑO Y SEGUÍ A TUS DUEÑOS");
        Tell("bathroom-clear", "¡LA PUERTA QUEDÓ LIBRE!", "Retiraste las tres prendas. Ya podés salir y continuar con tus dueños.", 1);
    }

    public void OnKitchenStarted()
    {
        SetObjective("OBJETIVO: AHUYENTÁ AL ALIENÍGENA · B / cuadrado (X en Xbox)");
        Tell("alien-a", "¡NO LO DEJES OPERAR!", "Acercate y ladrá mirando al alienígena. Cada ladrido que lo alcanza lo hace reaccionar; insistí para ahuyentarlo.");
    }

    public void OnAlienHidden()
    {
        SetObjective("OBJETIVO: BUSCÁ AL ALIENÍGENA Y HACETE ESCUCHAR");
        Tell("hidden", "¡SE ESCONDIÓ!", "Explorá y prestá atención a las pistas del entorno. Encontralo antes de volver a ladrarle.");
    }

    public void OnAlienDiscovered()
    {
        SetObjective("OBJETIVO: AHUYENTÁ AL ALIENÍGENA QUE ENCONTRASTE");
        Tell("discovered", "¡TE ESCUCHÓ!", "Encontraste al alienígena y tu ladrido lo alcanzó. Seguí ladrando hacia él para hacerlo salir.", 1);
    }

    public void OnRepairStarted()
    {
        SetObjective("OBJETIVO: INTERRUMPÍ AL ALIENÍGENA DEL CONTROL REMOTO");
        Tell("repair", "¡ESTÁ REPARANDO EL RAYO!", "El control remoto recupera la estabilidad de la energía. Acercate y ladrá al operador hasta detener la reparación.");
    }

    public void OnRepairInterrupted()
    {
        SetObjective("OBJETIVO: SEGUÍ A TUS DUEÑOS");
        Tell("repair-stopped", "¡INTERRUMPISTE LA REPARACIÓN!", "El operador dejó de usar el control. Seguí a tus dueños cuando continúen el recorrido.", 1);
    }

    public void OnKitchenCompleted()
    {
        SetObjective("OBJETIVO: ACOMPAÑÁ A TUS DUEÑOS AL DORMITORIO");
        Tell("return", "¡TUS DUEÑOS SIGUEN MOVIÉNDOSE!", "Acompañalos de regreso. Todavía hay algo extraño en la casa.");
    }

    public void OnClosingStarted()
    {
        closing = true;
        StopAllCoroutines(); presentation = null; pending.Clear();
        if (noticeGroup) noticeGroup.alpha = 0;
        if (objective) objective.text = "";
        CurrentNoticeId = null;
    }

    private void OnDisable()
    {
        if (beam) beam.InterferenceAccepted.RemoveListener(OnInterferenceAccepted);
        if (bathroom) { bathroom.AccidentStarted -= OnBathroomAccident; bathroom.ClothesReady -= OnClothesReady; }
        StopAllCoroutines(); presentation = null; pending.Clear(); CurrentNoticeId = null;
        if (noticeGroup) noticeGroup.alpha = 0;
    }
}

using System;
using UnityEngine;
using UnityEngine.Events;

public enum NarrativeEpisode { Bedroom, LivingRoom, Bathroom, Kitchen, ReturnAndClosing }
[Serializable] public sealed class EpisodeChangedEvent : UnityEvent<NarrativeEpisode> { }

// Componente local de la escena: hechos P1 y contratos para contenido futuro.
public sealed class EpisodeProgression : MonoBehaviour
{
    [SerializeField] private OwnersJourney ownersJourney;
    [SerializeField] private BathroomBasketDrop bathroomPuzzle;
    [SerializeField] private NarrativeEpisode currentEpisode = NarrativeEpisode.Bedroom;
    [Header("Habilitar solo cuando exista el contenido correspondiente")]
    [SerializeField] private bool livingRoomContentReady;
    [SerializeField] private bool bathroomContentReady = true;
    [SerializeField] private bool kitchenContentReady;
    [SerializeField] private bool returnContentReady;
    [SerializeField] private bool closingContentReady;
    [Header("Hechos observados, sin avance automático de episodios")]
    [SerializeField] private bool journeyStarted;
    [SerializeField] private bool bathroomSolved;
    [SerializeField] private bool closingRequested;
    [SerializeField] private EpisodeChangedEvent onEpisodeChanged = new EpisodeChangedEvent();
    [SerializeField] private UnityEvent onClosingRequested = new UnityEvent();
    public NarrativeEpisode CurrentEpisode => currentEpisode;
    public bool JourneyStarted => journeyStarted;
    public bool BathroomSolved => bathroomSolved;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        RecordJourneyStarted();
        RecordBathroomSolved();
    }

    public void RecordJourneyStarted()
    {
        if (ownersJourney && ownersJourney.HasStarted) journeyStarted = true;
    }

    public void RecordBathroomSolved()
    {
        if (bathroomPuzzle && bathroomPuzzle.IsSolved) bathroomSolved = true;
    }

    // Cada entrada exige su episodio esperado: repetir un evento no salta otra etapa.
    public void CompleteBedroom() => Advance(NarrativeEpisode.Bedroom, NarrativeEpisode.LivingRoom, livingRoomContentReady);
    public void CompleteLivingRoom() => Advance(NarrativeEpisode.LivingRoom, NarrativeEpisode.Bathroom, bathroomContentReady);
    public void CompleteBathroom()
    {
        RecordBathroomSolved();
        if (bathroomSolved) Advance(NarrativeEpisode.Bathroom, NarrativeEpisode.Kitchen, kitchenContentReady);
    }
    public void CompleteKitchen() => Advance(NarrativeEpisode.Kitchen, NarrativeEpisode.ReturnAndClosing, returnContentReady);

    private void Advance(NarrativeEpisode expected, NarrativeEpisode next, bool contentReady)
    {
        if (!contentReady || currentEpisode != expected) return;
        currentEpisode = next;
        onEpisodeChanged.Invoke(currentEpisode);
    }

    public void RequestClosing()
    {
        if (currentEpisode != NarrativeEpisode.ReturnAndClosing || !closingContentReady || closingRequested) return;
        closingRequested = true;
        onClosingRequested.Invoke();
    }
}

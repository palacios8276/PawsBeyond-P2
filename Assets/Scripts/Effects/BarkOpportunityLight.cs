using UnityEngine;

public sealed class BarkOpportunityLight : MonoBehaviour
{
    [SerializeField] private bool provokerOnly;
    [SerializeField] private KitchenEpisode kitchen;
    [SerializeField] private LivingRoomEpisode livingRoom;
    [SerializeField] private BathroomBasketDrop bathroom;
    [SerializeField] private Light cueLight;
    [SerializeField, Min(0f)] private float intensity = 0.45f;
    [SerializeField, Min(0.1f)] private float pulseSeconds = 1.6f;
    public bool IsShowing { get; private set; }
    private void Update()
    {
        IsShowing = provokerOnly ? kitchen.Step == KitchenEpisodeStep.FirstEncounter
            && kitchen.ActiveAlien && kitchen.ActiveAlien.State == KitchenAlienState.Provoking
            : livingRoom.IsWaitingForBarks || bathroom.IsAwaitingBark;
        float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * Mathf.PI * 2f / pulseSeconds);
        float acknowledgement = provokerOnly && kitchen.CurrentBarkCount > 0 ? 0.5f : 1f;
        cueLight.intensity = IsShowing ? intensity * pulse * acknowledgement : 0f;
    }
    private void OnDisable() { IsShowing = false; if (cueLight) cueLight.intensity = 0f; }
}

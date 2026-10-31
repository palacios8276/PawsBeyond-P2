using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
public sealed class AlienNavMeshMotor : MonoBehaviour
{
    [Header("Navegación de Alien A")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField, Min(0.1f)] private float speed = 4.5f;
    [SerializeField, Min(1f)] private float angularSpeed = 360f;
    [SerializeField, Min(0.1f)] private float acceleration = 18f;
    [SerializeField, Min(0.01f)] private float stoppingDistance = 0.08f;
    [SerializeField, Min(0.1f)] private float sampleDistance = 1.5f;
    [SerializeField, Min(0.5f)] private float waypointTimeout = 10f;
    [SerializeField, Min(0.1f)] private float stalledSeconds = 1.5f;
    [SerializeField, Min(0.001f)] private float minimumProgress = 0.04f;

    private readonly List<Vector3> preparedRoute = new List<Vector3>();
    private CharacterController characterController;

    public bool IsNavigating { get; private set; }
    public bool LastNavigationSucceeded { get; private set; }
    public NavMeshAgent Agent => agent;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        ConfigureAgent();
        agent.enabled = false;
    }

    private void OnValidate()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent) ConfigureAgent();
    }

    private void ConfigureAgent()
    {
        agent.speed = speed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    public bool PrepareRoute(Transform[] route)
    {
        preparedRoute.Clear();
        if (route == null || route.Length == 0 || !TrySample(transform.position, out Vector3 from)) return false;

        foreach (Transform point in route)
        {
            if (!point || !TrySample(point.position, out Vector3 destination)) return false;
            if (!HasCompletePath(from, destination)) return false;
            preparedRoute.Add(destination);
            from = destination;
        }
        return preparedRoute.Count > 0;
    }

    public IEnumerator TravelPreparedRoute()
    {
        LastNavigationSucceeded = false;
        if (preparedRoute.Count == 0 || !BeginNavigation()) yield break;

        IsNavigating = true;
        bool completed = true;
        foreach (Vector3 destination in preparedRoute)
        {
            yield return MoveTo(destination, stoppingDistance);
            if (!LastNavigationSucceeded)
            {
                completed = false;
                break;
            }
        }

        EndNavigation();
        LastNavigationSucceeded = completed;
    }

    public IEnumerator NavigateNear(Vector3 destination, float distance)
    {
        LastNavigationSucceeded = false;
        preparedRoute.Clear();
        if (!TrySample(transform.position, out Vector3 from) || !TrySample(destination, out Vector3 sampled)
            || !HasCompletePath(from, sampled)) yield break;

        preparedRoute.Add(sampled);
        if (!BeginNavigation()) yield break;
        IsNavigating = true;
        yield return MoveTo(sampled, Mathf.Max(stoppingDistance, distance));
        bool completed = LastNavigationSucceeded;
        EndNavigation();
        LastNavigationSucceeded = completed;
    }

    private IEnumerator MoveTo(Vector3 destination, float arrivalDistance)
    {
        LastNavigationSucceeded = false;
        agent.stoppingDistance = arrivalDistance;
        if (!agent.SetDestination(destination)) yield break;

        float deadline = Time.time + waypointTimeout;
        float stalledAt = Time.time;
        float previousRemaining = float.PositiveInfinity;

        while (agent.pathPending && Time.time < deadline) yield return null;
        if (agent.pathPending || agent.pathStatus != NavMeshPathStatus.PathComplete) yield break;

        while (Time.time < deadline)
        {
            float remaining = agent.remainingDistance;
            if (!agent.pathPending && remaining <= arrivalDistance + 0.04f)
            {
                LastNavigationSucceeded = true;
                yield break;
            }

            if (previousRemaining - remaining >= minimumProgress)
            {
                previousRemaining = remaining;
                stalledAt = Time.time;
            }
            else if (Time.time - stalledAt >= stalledSeconds)
                yield break;

            yield return null;
        }
    }

    private bool BeginNavigation()
    {
        if (!TrySample(transform.position, out Vector3 start)) return false;
        characterController.enabled = false;
        transform.position = start;
        agent.enabled = true;
        ConfigureAgent();
        if (!agent.isOnNavMesh && !agent.Warp(start))
        {
            agent.enabled = false;
            characterController.enabled = true;
            return false;
        }
        return true;
    }

    private void EndNavigation()
    {
        if (agent.enabled)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }
        characterController.enabled = true;
        IsNavigating = false;
    }

    private bool TrySample(Vector3 position, out Vector3 sampled)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
        {
            sampled = hit.position;
            return true;
        }
        sampled = position;
        return false;
    }

    private bool HasCompletePath(Vector3 from, Vector3 to)
    {
        var path = new NavMeshPath();
        return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete;
    }

    private void OnDisable()
    {
        if (agent && agent.enabled)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }
        if (characterController) characterController.enabled = true;
        IsNavigating = false;
    }
}

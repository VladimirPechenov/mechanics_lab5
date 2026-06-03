using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyStateMachine : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Suspicion,
        Alert
    }

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private float waypointTolerance = 0.35f;

    [Header("Hearing")]
    [SerializeField] private float hearingRange = 10f;

    [Header("Suspicion")]
    [SerializeField] private float suspicionDuration = 4.5f;

    [Header("Alert")]
    [SerializeField] private float attackRange = 1.7f;
    [SerializeField] private float attackCooldown = 1.1f;

    private NavMeshAgent agent;
    private EnemyVision vision;
    private Transform player;
    private int waypointIndex;
    private float suspicionTimer;
    private float lostPlayerTimer;
    private float lastAttackTime;
    private Vector3 lastKnownPosition;

    public State CurrentState { get; private set; } = State.Patrol;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<EnemyVision>();
    }

    private void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        MoveToCurrentWaypoint();
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case State.Patrol:
                Patrol();
                ListenForNoise();
                break;
            case State.Suspicion:
                Investigate();
                ListenForNoise();
                break;
            case State.Alert:
                ChasePlayer();
                break;
        }
    }

    public void Configure(Transform[] patrolWaypoints, float hearing)
    {
        waypoints = patrolWaypoints;
        hearingRange = hearing;
    }

    public void OnPlayerDetected(Vector3 playerPosition)
    {
        lastKnownPosition = playerPosition;
        lostPlayerTimer = 0f;
        SetState(State.Alert);
    }

    public void OnPlayerLost()
    {
        if (CurrentState != State.Alert)
        {
            return;
        }

        lostPlayerTimer += Time.deltaTime;
        if (lostPlayerTimer < 1.3f)
        {
            return;
        }

        suspicionTimer = suspicionDuration;
        agent.SetDestination(lastKnownPosition);
        SetState(State.Suspicion);
    }

    private void Patrol()
    {
        if (waypoints.Length == 0)
        {
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, waypointTolerance))
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            MoveToCurrentWaypoint();
        }
    }

    private void Investigate()
    {
        suspicionTimer -= Time.deltaTime;
        transform.Rotate(Vector3.up, 50f * Time.deltaTime);

        var reachedPoint = !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, waypointTolerance);
        if (suspicionTimer <= 0f || reachedPoint)
        {
            SetState(State.Patrol);
            MoveToCurrentWaypoint();
        }
    }

    private void ChasePlayer()
    {
        if (player == null)
        {
            return;
        }

        if (vision != null && vision.CanSeePlayer)
        {
            lastKnownPosition = player.position;
        }

        agent.SetDestination(lastKnownPosition);

        if (Vector3.Distance(transform.position, player.position) <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Debug.Log($"{name}: player caught in stealth lab scene.");
        }
    }

    private void ListenForNoise()
    {
        if (!NoiseManager.TryGetClosestNoise(transform.position, hearingRange, out var noise))
        {
            return;
        }

        lastKnownPosition = noise.Position;
        suspicionTimer = suspicionDuration + noise.Intensity;
        agent.SetDestination(lastKnownPosition);
        SetState(State.Suspicion);
    }

    private void MoveToCurrentWaypoint()
    {
        if (waypoints.Length == 0 || waypoints[waypointIndex] == null)
        {
            return;
        }

        agent.SetDestination(waypoints[waypointIndex].position);
    }

    private void SetState(State state)
    {
        CurrentState = state;
    }
}

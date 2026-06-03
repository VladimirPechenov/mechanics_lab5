using UnityEngine;

[RequireComponent(typeof(EnemyStateMachine))]
public class EnemyVision : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField] private float viewRadius = 9f;
    [SerializeField] private float viewAngle = 92f;
    [SerializeField] private float eyeHeight = 1.45f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Transform player;

    private EnemyStateMachine stateMachine;
    private float lastSeenTime;

    public float ViewRadius => viewRadius;
    public float ViewAngle => viewAngle;
    public bool CanSeePlayer { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; }

    private Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

    private void Awake()
    {
        stateMachine = GetComponent<EnemyStateMachine>();
    }

    private void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }
    }

    private void Update()
    {
        CanSeePlayer = CheckPlayerVisibility(out var visiblePosition);

        if (CanSeePlayer)
        {
            lastSeenTime = Time.time;
            LastKnownPlayerPosition = visiblePosition;
            stateMachine.OnPlayerDetected(visiblePosition);
        }
        else if (Time.time - lastSeenTime > 0.35f)
        {
            stateMachine.OnPlayerLost();
        }
    }

    public void Configure(Transform target, LayerMask obstacles, float radius, float angle)
    {
        player = target;
        obstacleMask = obstacles;
        viewRadius = radius;
        viewAngle = angle;
    }

    private bool CheckPlayerVisibility(out Vector3 visiblePosition)
    {
        visiblePosition = Vector3.zero;

        if (player == null)
        {
            return false;
        }

        var targetPosition = player.position + Vector3.up * 0.9f;
        var direction = targetPosition - EyePosition;
        var distance = direction.magnitude;

        if (distance > viewRadius)
        {
            return false;
        }

        var flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        if (Vector3.Angle(transform.forward, flatDirection) > viewAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(EyePosition, direction.normalized, out var hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(EyePosition, hit.point, Color.red);
            return false;
        }

        Debug.DrawLine(EyePosition, targetPosition, Color.green);
        visiblePosition = player.position;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        var left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * viewRadius;
        var right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * viewRadius;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(EyePosition, EyePosition + left);
        Gizmos.DrawLine(EyePosition, EyePosition + right);
    }
}

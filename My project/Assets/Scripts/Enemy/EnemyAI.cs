using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public EscapeChallenge escapeChallenge;
    public float detectionRange = 30f;
    public float catchDistance = 1.5f;
    public float closeExposureMultiplier = 3f;
    [Tooltip("A lit flashlight makes the player easier to spot.")]
    public float flashlightExposureMultiplier = 1.5f;
    public LayerMask lineOfSightLayers = ~0;
    public float eyeHeight = 1.5f;
    [Tooltip("Total width of the enemy's field of view in degrees, centred on where it faces.")]
    [Range(10f, 360f)] public float viewAngle = 100f;
    [Tooltip("Degrees per second the enemy turns on the spot while looking around.")]
    public float scanSpeed = 45f;

    [Header("Movement")]
    public float repathInterval = 0.2f;
    [Tooltip("Roaming speed as a fraction of the agent's speed, which is used for chasing.")]
    public float roamSpeedMultiplier = 0.5f;

    [Header("Roaming")]
    public float roamRadius = 16f;
    public float roamPauseMin = 1.5f;
    public float roamPauseMax = 4f;
    [Range(0f, 1f)] public float roamTowardPlayerChance = 0.25f;

    [Header("Investigating")]
    public float investigateLookSeconds = 3f;
    public float investigateTimeout = 15f;
    public float foundDistance = 2.5f;
    [Tooltip("How fast exposure drains, as a multiple of the exposure rate, while the enemy cannot see the player.")]
    public float exposureRecovery = 0.5f;

    enum State { Roam, Investigate, Chase }

    NavMeshAgent agent;
    PlayerMovement playerMovement;
    State state = State.Roam;
    float chaseSpeed;
    float nextRepathTime;
    float nextPlaceAttempt;
    float roamWaitUntil;
    float investigateGiveUpTime;
    float investigateLookUntil;
    bool investigateKnowsHidingSpot;
    Vector3 lastSeenPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            chaseSpeed = agent.speed;
    }

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (escapeChallenge == null)
            escapeChallenge = FindAnyObjectByType<EscapeChallenge>();

        if (target != null)
            playerMovement = target.GetComponent<PlayerMovement>();

        if (agent == null)
        {
            Debug.LogError("EnemyAI requires a NavMeshAgent component.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
            agent.Warp(navHit.position);

        if (!agent.isOnNavMesh)
            Debug.LogWarning("Enemy is not on a NavMesh. Check that Ground and Stairs have colliders and are included in the NavMesh layers.", this);

        if (playerMovement != null)
            playerMovement.PanicAlert += OnPanicAlert;
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.PanicAlert -= OnPanicAlert;
    }

    void Update()
    {
        if (agent == null || target == null)
            return;

        if (!agent.isOnNavMesh)
        {
            TryPlaceOnNavMesh();
            return;
        }

        if (playerMovement == null)
        {
            playerMovement = target.GetComponent<PlayerMovement>();
            if (playerMovement == null)
                return;

            playerMovement.PanicAlert += OnPanicAlert;
        }

        bool hidden = playerMovement.IsHidden;
        float distance = Vector3.Distance(transform.position, target.position);

        if (!hidden && distance <= catchDistance)
        {
            agent.isStopped = true;
            escapeChallenge?.Fail();
            return;
        }

        if (state != State.Chase)
        {
            if (!hidden && CanSeeTarget())
            {
                float proximity = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, detectionRange));
                float exposureMultiplier = Mathf.Lerp(1f, closeExposureMultiplier, proximity);
                float lightMultiplier = playerMovement.FlashlightOn ? flashlightExposureMultiplier : 1f;
                playerMovement.IncreaseExposure(playerMovement.exposureRate * exposureMultiplier * lightMultiplier * Time.deltaTime);

                if (playerMovement.IsFullyExposed)
                {
                    state = State.Chase;
                    lastSeenPosition = target.position;
                }
            }
            else
            {
                playerMovement.DecreaseExposure(playerMovement.exposureRate * exposureRecovery * Time.deltaTime);
            }
        }

        switch (state)
        {
            case State.Chase:
                UpdateChase(hidden);
                break;
            case State.Investigate:
                UpdateInvestigate(hidden, distance);
                break;
            default:
                UpdateRoam();
                break;
        }
    }

    void TryPlaceOnNavMesh()
    {
        if (Time.time < nextPlaceAttempt)
            return;

        nextPlaceAttempt = Time.time + 0.5f;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, agent.areaMask))
            agent.Warp(hit.position);
    }

    void UpdateChase(bool hidden)
    {
        agent.speed = chaseSpeed;

        // The player vanished into a hiding spot, so go to where they were last seen.
        if (hidden)
        {
            BeginInvestigate(lastSeenPosition, false);
            return;
        }

        lastSeenPosition = target.position;
        agent.isStopped = false;
        if (Time.time < nextRepathTime)
            return;

        SetDestinationNear(target.position);
        nextRepathTime = Time.time + repathInterval;
    }

    void UpdateInvestigate(bool hidden, float distance)
    {
        agent.speed = chaseSpeed;

        if (investigateKnowsHidingSpot && hidden && distance <= foundDistance)
        {
            agent.isStopped = true;
            escapeChallenge?.Fail();
            return;
        }

        bool arrived = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
        if (arrived && investigateLookUntil <= 0f)
        {
            investigateLookUntil = Time.time + investigateLookSeconds;
            agent.isStopped = true;
        }

        if (investigateLookUntil > 0f)
            ScanAround();

        bool finishedLooking = investigateLookUntil > 0f && Time.time >= investigateLookUntil;
        if (finishedLooking || Time.time >= investigateGiveUpTime)
            EndInvestigate();
    }

    void UpdateRoam()
    {
        agent.speed = chaseSpeed * roamSpeedMultiplier;
        agent.isStopped = false;

        bool arrived = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.3f);
        if (!arrived)
            return;

        if (roamWaitUntil <= 0f)
            roamWaitUntil = Time.time + Random.Range(roamPauseMin, roamPauseMax);

        if (Time.time < roamWaitUntil)
        {
            ScanAround();
            return;
        }

        roamWaitUntil = 0f;
        PickRoamDestination();
    }

    void PickRoamDestination()
    {
        Vector3 center = Random.value < roamTowardPlayerChance ? target.position : transform.position;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 offset = Random.insideUnitSphere;
            offset.y *= 0.4f;

            if (!NavMesh.SamplePosition(center + offset * roamRadius, out NavMeshHit hit, 6f, agent.areaMask))
                continue;

            if (Vector3.Distance(hit.position, transform.position) < 3f)
                continue;

            var path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return;
            }
        }
    }

    void BeginInvestigate(Vector3 point, bool knowsHidingSpot)
    {
        state = State.Investigate;
        investigateKnowsHidingSpot = knowsHidingSpot;
        investigateGiveUpTime = Time.time + investigateTimeout;
        investigateLookUntil = 0f;

        agent.speed = chaseSpeed;
        agent.isStopped = false;
        SetDestinationNear(point);
    }

    void EndInvestigate()
    {
        state = State.Roam;
        roamWaitUntil = 0f;
        playerMovement.ResetExposure();
        agent.ResetPath();
        agent.isStopped = false;
    }

    // The player panicked while hiding, which gives away the exact hiding spot.
    void OnPanicAlert(Vector3 position)
    {
        if (agent == null || !agent.isOnNavMesh || state == State.Chase)
            return;

        BeginInvestigate(position, true);
    }

    // Positions can sit inside a carved obstacle or above the mesh, so snap them first.
    void SetDestinationNear(Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, agent.areaMask))
            position = hit.position;

        agent.SetDestination(position);
    }

    void ScanAround()
    {
        transform.Rotate(0f, scanSpeed * Time.deltaTime, 0f);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward * detectionRange;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward * detectionRange;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + left);
        Gizmos.DrawLine(origin, origin + right);
        Gizmos.DrawLine(origin + left, origin + right);
    }

    bool CanSeeTarget()
    {
        if (playerMovement != null && playerMovement.IsHidden)
            return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = target.position + Vector3.up;
        Vector3 direction = targetPosition - origin;
        float distance = direction.magnitude;

        if (distance > detectionRange || distance <= 0f)
            return false;

        // Only what is inside the field of view in front of the enemy can be seen.
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, flatDirection) > viewAngle * 0.5f)
            return false;

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, lineOfSightLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.transform == target || hit.transform.IsChildOf(target);
    }
}

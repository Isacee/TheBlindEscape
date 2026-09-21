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
    public LayerMask lineOfSightLayers = ~0;
    public float eyeHeight = 1.5f;

    [Header("Movement")]
    public float repathInterval = 0.2f;

    NavMeshAgent agent;
    PlayerMovement playerMovement;
    float nextRepathTime;
    bool isChasing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
    }

    void Update()
    {
        if (agent == null || target == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        if (playerMovement != null && playerMovement.IsHidden)
        {
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (!isChasing)
        {
            if (playerMovement == null)
                playerMovement = target.GetComponent<PlayerMovement>();

            if (playerMovement == null)
            {
                agent.isStopped = true;
                return;
            }

            if (CanSeeTarget())
            {
                float proximity = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, detectionRange));
                float exposureMultiplier = Mathf.Lerp(1f, closeExposureMultiplier, proximity);
                playerMovement.IncreaseExposure(playerMovement.exposureRate * exposureMultiplier * Time.deltaTime);
                isChasing = playerMovement.IsFullyExposed;
            }

            if (!isChasing)
            {
                agent.isStopped = true;
                return;
            }
        }

        if (distance <= catchDistance)
        {
            agent.isStopped = true;
            escapeChallenge?.Fail();
            return;
        }

        agent.isStopped = false;
        if (Time.time < nextRepathTime)
            return;

        agent.SetDestination(target.position);
        nextRepathTime = Time.time + repathInterval;
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

        if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, lineOfSightLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.transform == target || hit.transform.IsChildOf(target);
    }
}

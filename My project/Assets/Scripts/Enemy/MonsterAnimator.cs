using UnityEngine;
using UnityEngine.AI;

// Procedural animation for the primitive-built monster: stride, sway, head twitch and a hunched lean.
public class MonsterAnimator : MonoBehaviour
{
    public Transform hips;
    public Transform head;
    public Transform armLeft;
    public Transform armRight;
    public Transform legLeft;
    public Transform legRight;

    public float strideRate = 2.2f;
    public float referenceSpeed = 3.5f;

    NavMeshAgent agent;
    Vector3 hipsBase;
    float phase;
    float seed;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        seed = Random.value * 100f;
        if (hips != null)
            hipsBase = hips.localPosition;
    }

    void Update()
    {
        float speed = agent != null ? agent.velocity.magnitude : 0f;
        float walk = Mathf.Clamp01(speed / Mathf.Max(0.1f, referenceSpeed));
        float t = Time.time + seed;

        phase += speed * strideRate * Time.deltaTime;
        float swing = Mathf.Sin(phase) * 38f * walk;

        if (legLeft != null) legLeft.localRotation = Quaternion.Euler(swing, 0f, 0f);
        if (legRight != null) legRight.localRotation = Quaternion.Euler(-swing, 0f, 0f);

        // Arms hang loose, drag behind the stride and sway even when standing still.
        float idle = Mathf.Sin(t * 1.3f) * 4f;
        if (armLeft != null) armLeft.localRotation = Quaternion.Euler(-swing * 0.8f + idle, 0f, -8f - Mathf.Sin(t * 0.9f) * 4f);
        if (armRight != null) armRight.localRotation = Quaternion.Euler(swing * 0.8f - idle, 0f, 8f + Mathf.Sin(t * 1.1f) * 4f);

        if (hips != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.06f * walk + Mathf.Sin(t * 1.5f) * 0.01f;
            hips.localPosition = hipsBase + new Vector3(0f, bob, 0f);
            hips.localRotation = Quaternion.Euler(8f + walk * 14f, Mathf.Sin(phase) * 5f * walk, 0f);
        }

        if (head != null)
        {
            float twitch = (Mathf.PerlinNoise(t * 3f, 0f) - 0.5f) * 30f;
            float yaw = Mathf.Sin(t * 0.7f) * 18f + twitch;
            float tilt = Mathf.Sin(t * 0.5f) * 8f + (Mathf.PerlinNoise(0f, t * 2f) - 0.5f) * 14f;
            head.localRotation = Quaternion.Euler(-10f, yaw, tilt);
        }
    }
}

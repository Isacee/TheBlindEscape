using UnityEngine;

public class PlayerGrounding : MonoBehaviour
{
    Transform playerTransform;
    Collider playerCollider;
    LayerMask groundLayers;
    float groundCheckDistance;
    float maxGroundAngle;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;

    public void Initialize(
        Transform player,
        Collider collider,
        LayerMask layers,
        float checkDistance,
        float maximumGroundAngle)
    {
        playerTransform = player;
        playerCollider = collider;
        groundLayers = layers;
        groundCheckDistance = checkDistance;
        maxGroundAngle = maximumGroundAngle;
    }

    public void CheckGrounded()
    {
        GroundNormal = Vector3.up;

        if (playerCollider == null)
        {
            IsGrounded = CheckGroundAt(
                playerTransform.position,
                groundCheckDistance + 0.05f,
                out Vector3 fallbackNormal);
            GroundNormal = fallbackNormal;
            return;
        }

        Bounds bounds = playerCollider.bounds;
        Vector3 feetCenter = new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
        float rayDistance = groundCheckDistance + 0.05f;
        float checkRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;
        Vector3 hitNormal;

        IsGrounded = CheckGroundAt(feetCenter, rayDistance, out hitNormal);
        GroundNormal = hitNormal;

        if (!IsGrounded)
        {
            Vector3[] offsets =
            {
                Vector3.forward * checkRadius,
                Vector3.back * checkRadius,
                Vector3.left * checkRadius,
                Vector3.right * checkRadius
            };

            foreach (Vector3 offset in offsets)
            {
                if (CheckGroundAt(feetCenter + offset, rayDistance, out hitNormal))
                {
                    GroundNormal = hitNormal;
                    IsGrounded = true;
                    break;
                }
            }
        }
    }

    public bool IsWalkable(Vector3 normal)
    {
        return Vector3.Angle(normal, Vector3.up) <= maxGroundAngle;
    }

    bool CheckGroundAt(Vector3 origin, float distance, out RaycastHit groundHit)
    {
        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out groundHit,
                distance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            return false;

        if (groundHit.collider.transform == playerTransform || groundHit.collider.transform.IsChildOf(playerTransform))
            return false;

        return IsWalkable(groundHit.normal);
    }

    bool CheckGroundAt(Vector3 origin, float distance, out Vector3 hitNormal)
    {
        hitNormal = Vector3.up;
        bool foundGround = CheckGroundAt(origin, distance, out RaycastHit groundHit);
        if (foundGround)
            hitNormal = groundHit.normal;
        return foundGround;
    }
}

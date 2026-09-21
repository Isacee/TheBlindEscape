using UnityEngine;

public class HideSpot : MonoBehaviour
{
    public Transform hidePoint;
    public Transform exitPoint;
    public float interactionRadius = 2f;

    public Vector3 GetHidePosition()
    {
        return hidePoint != null ? hidePoint.position : transform.position;
    }

    public Vector3 GetExitPosition()
    {
        if (exitPoint != null)
            return exitPoint.position;

        Vector3 exitDirection = hidePoint != null ? hidePoint.forward : transform.forward;
        return transform.position + exitDirection * 1.5f + Vector3.up;
    }
}

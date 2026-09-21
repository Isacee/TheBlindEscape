using UnityEngine;

public class HideSpotSetup : MonoBehaviour
{
    [Header("Hide Point Setup")]
    public Vector3 hideOffset = new Vector3(0f, 0f, -1.2f);
    public Vector3 exitOffset = new Vector3(1.5f, 0f, 0f);
    public bool createOnAwake = true;

    void Reset()
    {
        EnsurePoints();
    }

    void Awake()
    {
        if (createOnAwake)
            EnsurePoints();
    }

    [ContextMenu("Create Hide Points")]
    public void EnsurePoints()
    {
        Transform hide = transform.Find("HidePoint");
        if (hide == null)
        {
            hide = new GameObject("HidePoint").transform;
            hide.SetParent(transform);
        }
        hide.localPosition = hideOffset;

        Transform exit = transform.Find("ExitPoint");
        if (exit == null)
        {
            exit = new GameObject("ExitPoint").transform;
            exit.SetParent(transform);
        }
        exit.localPosition = exitOffset;

        HideSpot hideSpot = GetComponent<HideSpot>();
        if (hideSpot == null)
            hideSpot = gameObject.AddComponent<HideSpot>();

        hideSpot.hidePoint = hide;
        hideSpot.exitPoint = exit;
        if (hideSpot.interactionRadius <= 0.01f)
            hideSpot.interactionRadius = 2f;
    }
}

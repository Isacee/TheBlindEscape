using System.Collections.Generic;
using UnityEngine;

// Scatters the keys at random among hand-placed points inside the house, away from the player and from each other.
public class KeySpawner : MonoBehaviour
{
    public GameObject keyPrefab;
    public int keyCount = 3;
    [Tooltip("Keys never spawn closer than this to where the player starts.")]
    public float minDistanceFromPlayer = 12f;
    public float minDistanceBetweenKeys = 8f;

    void Start()
    {
        var points = new List<Transform>();
        foreach (Transform child in transform)
            points.Add(child);

        if (keyPrefab == null || points.Count == 0)
            return;

        keyCount = Mathf.Min(keyCount, points.Count);

        var challenge = FindAnyObjectByType<EscapeChallenge>();
        if (challenge != null)
            challenge.keysRequired = keyCount;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPosition = player != null ? player.transform.position : Vector3.positiveInfinity;

        foreach (Transform point in Choose(points, playerPosition))
        {
            GameObject key = Instantiate(keyPrefab, point.position, Quaternion.identity, transform);
            key.name = "Key";
        }
    }

    List<Transform> Choose(List<Transform> points, Vector3 playerPosition)
    {
        float playerDistance = minDistanceFromPlayer;
        float keyDistance = minDistanceBetweenKeys;
        var chosen = new List<Transform>();

        // If the rules cannot be met with the points available, relax them a little and retry.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Shuffle(points);
            chosen.Clear();

            foreach (Transform point in points)
            {
                if (Vector3.Distance(point.position, playerPosition) < playerDistance)
                    continue;

                if (TooCloseToChosen(point, chosen, keyDistance))
                    continue;

                chosen.Add(point);
                if (chosen.Count == keyCount)
                    return chosen;
            }

            playerDistance *= 0.75f;
            keyDistance *= 0.75f;
        }

        // Last resort: take the points farthest from the player.
        points.Sort((a, b) => Vector3.Distance(b.position, playerPosition).CompareTo(Vector3.Distance(a.position, playerPosition)));
        return points.GetRange(0, keyCount);
    }

    static bool TooCloseToChosen(Transform point, List<Transform> chosen, float minDistance)
    {
        foreach (Transform other in chosen)
        {
            if (Vector3.Distance(point.position, other.position) < minDistance)
                return true;
        }

        return false;
    }

    static void Shuffle(List<Transform> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.9f, 0.8f, 0.45f, 0.8f);
        foreach (Transform child in transform)
            Gizmos.DrawWireSphere(child.position, 0.3f);
    }
}

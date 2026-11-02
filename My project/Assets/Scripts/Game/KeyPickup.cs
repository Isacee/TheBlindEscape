using UnityEngine;

// A floating, matte key. Walk up to it to collect it; it has no glow or light on purpose.
public class KeyPickup : MonoBehaviour
{
    public Transform model;
    public float floatAmplitude = 0.1f;
    public float floatSpeed = 1.5f;
    public float spinSpeed = 60f;
    public float pickupRadius = 1.3f;

    Transform player;
    EscapeChallenge challenge;
    Vector3 modelBasePosition;
    float phase;

    void Start()
    {
        if (model == null && transform.childCount > 0)
            model = transform.GetChild(0);

        if (model != null)
            modelBasePosition = model.localPosition;

        phase = Random.value * Mathf.PI * 2f;
        challenge = FindAnyObjectByType<EscapeChallenge>();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (model != null)
        {
            model.localPosition = modelBasePosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed + phase) * floatAmplitude);
            model.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
                return;

            player = playerObject.transform;
        }

        // The player pivot is at the feet, so measure from around waist height.
        if (Vector3.Distance(player.position + Vector3.up * 0.9f, transform.position) > pickupRadius)
            return;

        if (challenge != null)
            challenge.CollectKey();

        Destroy(gameObject);
    }
}

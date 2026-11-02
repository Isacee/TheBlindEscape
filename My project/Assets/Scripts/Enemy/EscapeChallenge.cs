using UnityEngine;

public class EscapeChallenge : MonoBehaviour
{
    [Header("Challenge")]
    public Transform player;
    public Transform pursuer;
    public float timeLimit = 600f;
    [Tooltip("Keys that must be collected to escape. Running out of time is a loss.")]
    public int keysRequired = 3;
    public bool startOnEnable = true;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }
    public bool HasEscaped { get; private set; }
    public bool HasFailed { get; private set; }
    public bool TimedOut { get; private set; }
    public int KeysCollected { get; private set; }
    public int KeysRequired => keysRequired;

    void OnEnable()
    {
        if (startOnEnable)
            Begin();
    }

    void Update()
    {
        if (!IsRunning)
            return;

        TimeRemaining -= Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            HasFailed = true;
            TimedOut = true;
            IsRunning = false;
        }
    }

    public void CollectKey()
    {
        if (!IsRunning)
            return;

        KeysCollected++;
        if (KeysCollected >= keysRequired)
        {
            HasEscaped = true;
            IsRunning = false;
        }
    }

    public void Begin()
    {
        TimeRemaining = timeLimit;
        IsRunning = true;
        HasEscaped = false;
        HasFailed = false;
        TimedOut = false;
        KeysCollected = 0;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Fail()
    {
        if (!IsRunning)
            return;

        TimeRemaining = 0f;
        HasFailed = true;
        IsRunning = false;
    }
}

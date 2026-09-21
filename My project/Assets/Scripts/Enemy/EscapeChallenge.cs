using UnityEngine;

public class EscapeChallenge : MonoBehaviour
{
    [Header("Challenge")]
    public Transform player;
    public Transform pursuer;
    public float timeLimit = 30f;
    public bool startOnEnable = true;

    public float TimeRemaining { get; private set; }
    public bool IsRunning { get; private set; }
    public bool HasEscaped { get; private set; }
    public bool HasFailed { get; private set; }

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

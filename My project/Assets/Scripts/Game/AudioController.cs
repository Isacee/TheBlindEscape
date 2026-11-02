using UnityEngine;

// Plays the looping soundscape and reacts to hiding and panic. Clips that are left empty are skipped.
public class AudioController : MonoBehaviour
{
    [Header("Loops")]
    public AudioClip ambientLoop;
    [Range(0f, 1f)] public float ambientVolume = 0.5f;
    public AudioClip windLoop;
    [Range(0f, 1f)] public float windVolume = 0.35f;
    public AudioClip whispersLoop;
    [Range(0f, 1f)] public float whispersMaxVolume = 0.8f;
    [Tooltip("How quickly the whispers fade in and out, in volume per second.")]
    public float whispersFadeSpeed = 0.5f;

    [Header("Effects")]
    public AudioClip doorClip;
    public AudioClip panicClip;
    [Range(0f, 1f)] public float effectVolume = 0.8f;

    AudioSource whispers;
    AudioSource effects;
    PlayerMovement player;
    float whisperLevel;

    void Start()
    {
        CreateLoop(ambientLoop, ambientVolume);
        CreateLoop(windLoop, windVolume);
        whispers = CreateLoop(whispersLoop, 0f);

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;
    }

    void Update()
    {
        AudioListener.pause = Time.timeScale == 0f;

        if (player == null)
            BindPlayer();

        if (whispers == null)
            return;

        // Whispers swell with panic and, more quietly, with exposure.
        float target = 0f;
        if (player != null)
        {
            float exposure = Mathf.Clamp01(player.Exposure / Mathf.Max(0.01f, player.exposureToChase));
            target = Mathf.Max(player.Panic, exposure * 0.6f);
        }

        whisperLevel = Mathf.MoveTowards(whisperLevel, target, whispersFadeSpeed * Time.deltaTime);
        whispers.volume = whisperLevel * whispersMaxVolume;
    }

    void OnDestroy()
    {
        AudioListener.pause = false;

        if (player != null)
        {
            player.HideChanged -= OnHideChanged;
            player.PanicAlert -= OnPanicAlert;
        }
    }

    void BindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null || !playerObject.TryGetComponent(out player))
            return;

        player.HideChanged += OnHideChanged;
        player.PanicAlert += OnPanicAlert;
    }

    void OnHideChanged(bool hidden)
    {
        Play(doorClip);
    }

    void OnPanicAlert(Vector3 position)
    {
        Play(panicClip);
    }

    void Play(AudioClip clip)
    {
        if (clip != null && effects != null)
            effects.PlayOneShot(clip, effectVolume);
    }

    AudioSource CreateLoop(AudioClip clip, float volume)
    {
        if (clip == null)
            return null;

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.Play();
        return source;
    }
}

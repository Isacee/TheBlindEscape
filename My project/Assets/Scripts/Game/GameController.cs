using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

public class GameController : MonoBehaviour
{
    [Header("Scene References")]
    public EscapeChallenge escapeChallenge;
    public GameUI gameUI;

    [Header("Scene Settings")]
    public string menuSceneName = "Menu";
    public bool pauseWithEscape = true;
    public LayerMask navMeshLayers = (1 << 3) | (1 << 7);

    public bool IsPaused { get; private set; }
    public bool HasWon { get; private set; }
    public bool HasLost { get; private set; }

    void Awake()
    {
        Time.timeScale = 1f;

        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = gameObject.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.All;
        surface.layerMask = navMeshLayers;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }

    void Start()
    {
        if (gameUI == null)
            gameUI = GetComponent<GameUI>();

        if (gameUI == null)
            gameUI = gameObject.AddComponent<GameUI>();

        gameUI.Initialize(this);
    }

    void Update()
    {
        if (pauseWithEscape && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (escapeChallenge == null)
            return;

        if (escapeChallenge.HasEscaped)
        {
            if (!HasWon)
                SetPaused(true);
            HasWon = true;
        }
        else if (escapeChallenge.HasFailed)
        {
            if (!HasLost)
                SetPaused(true);
            HasLost = true;
        }

        gameUI?.UpdateUI(escapeChallenge);
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

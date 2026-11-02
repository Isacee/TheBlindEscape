using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

[DefaultExecutionOrder(-1000)]
public class GameController : MonoBehaviour
{
    const int FloorLayer = 3;
    const int NotWalkableArea = 1;

    [Header("Scene References")]
    public EscapeChallenge escapeChallenge;
    public GameUI gameUI;

    [Header("Scene Settings")]
    public string menuSceneName = "Menu";
    public bool pauseWithEscape = true;
    public LayerMask navMeshLayers = (1 << 3) | (1 << 7);

    [Header("NavMesh Bounds")]
    [Tooltip("Only geometry inside this box can become walkable, so nothing outside the house is ever pathable.")]
    public Vector3 navMeshCenter = new Vector3(0f, 3.5f, 0f);
    public Vector3 navMeshSize = new Vector3(27.6f, 9f, 27.6f);

    public bool IsPaused { get; private set; }
    public bool HasWon { get; private set; }
    public bool HasLost { get; private set; }

    // Runs before every agent is created so the navmesh already exists when they start.
    void Awake()
    {
        Time.timeScale = 1f;

        NavMeshSurface surface = GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = gameObject.AddComponent<NavMeshSurface>();

        ConvertObstaclesToNavMeshVolumes();

        surface.collectObjects = CollectObjects.Volume;
        surface.center = navMeshCenter;
        surface.size = navMeshSize;

        // A serialized mask of 0 collects nothing and leaves the enemy without a navmesh.
        int walkableMask = navMeshLayers.value != 0 ? navMeshLayers.value : (1 << 3) | (1 << 7);
        surface.layerMask = walkableMask | (1 << FloorLayer);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
    }

    // Thousands of overlapping runtime carves are slow and can corrupt the mesh, so bake them in instead.
    void ConvertObstaclesToNavMeshVolumes()
    {
        foreach (NavMeshObstacle obstacle in FindObjectsByType<NavMeshObstacle>())
        {
            if (obstacle.shape != NavMeshObstacleShape.Box)
                continue;

            obstacle.enabled = false;

            var block = new GameObject("NavBlock");
            block.layer = FloorLayer;
            block.transform.SetParent(obstacle.transform, false);

            var volume = block.AddComponent<NavMeshModifierVolume>();
            volume.center = obstacle.center;
            volume.size = obstacle.size;
            volume.area = NotWalkableArea;
        }
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class gameMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "House";

    // Coming back from the game leaves the cursor locked and time paused.
    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}

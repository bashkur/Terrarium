using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Game Manager is a Singleton
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance != null) return instance;
            // lazy loading, kind of
            instance = new GameObject().AddComponent<GameManager>();
            instance.name = instance.GetType().ToString();
            DontDestroyOnLoad(instance.gameObject);
            return instance;
        }
    }


    [HideInInspector]
    public enum GameState
    {
        MainMenu,
        Running,
        Paused
    }
    public GameState gameState = GameState.MainMenu;

    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneFinishedLoading;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneFinishedLoading;
    }

    
    private void OnSceneFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        // SceneManager.SetActiveScene(scene);
        Debug.Log($"{scene.name} finished loading");
    }
    
    public void StartGame()
    {
        gameState = GameState.Running;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadSceneAsync("BashSandbox");
    }

    public void PauseGame(GameObject pauseMenu)
    {
        if (gameState != GameState.Running) return;

        Time.timeScale = 0;
        gameState = GameState.Paused;
        pauseMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public void ResumeGame(GameObject pauseMenu)
    {
        if (gameState != GameState.Paused) return;
        
        Time.timeScale = 1;
        gameState = GameState.Running;
        pauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
        Application.Quit();
    }
}
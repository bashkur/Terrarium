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


    public enum GameState
    {
        MainMenu,
        Running,
        Paused,
        GameOver
    }
    public GameState gameState = GameState.MainMenu;

    public delegate void OnScoreChange(int newVal);
    public event OnScoreChange OnScoreChanged;
    private int score;
    public int Score
    {
        get => score;
        private set
        {
            if (score == value) return;
            score = value;
            OnScoreChanged?.Invoke(score);
        }
    }

    private void Awake()
    {
        instance = this;
        Score = 0;
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
        // TODO: might have to use this when the scene is heavier and takes longer to load
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

    public void UpdateScore(int amount)
    {
        Score += amount;
        Debug.Log($"Score: {Score}");
    }
}
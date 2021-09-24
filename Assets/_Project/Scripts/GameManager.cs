using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton

    private static GameManager instance;

    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject().AddComponent<GameManager>();
                instance.name = instance.GetType().ToString();
                DontDestroyOnLoad(instance.gameObject);
            }
            return instance;
        }
    }

    #endregion

    private static bool isPaused;

    public static bool IsPaused => isPaused;

    private void Awake()
    {
        isPaused = false;
    }

    public void TogglePauseGame()
    {
        isPaused = !isPaused;
        Time.timeScale = Time.timeScale <= Mathf.Epsilon ? 0 : 1;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif
        Application.Quit();
    }
}
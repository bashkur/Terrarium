using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject quitDialog;


    private GameManager gameManager;

    private void Awake()
    {
        pauseMenu.SetActive(false);
        quitDialog.SetActive(false);
    }

    void Start()
    {
        gameManager = GameManager.Instance;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (gameManager.gameState)
            {
                case GameManager.GameState.Running:
                    gameManager.PauseGame(pauseMenu);
                    break;
                case GameManager.GameState.Paused:
                    gameManager.ResumeGame(pauseMenu);
                    break;
                case GameManager.GameState.MainMenu:
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid Game State: {gameManager.gameState.ToString()}");
            }
        }
    }

    public void ResumeGame()
    {
        gameManager.ResumeGame(pauseMenu);
    }

    public void LoadMainMenu()
    {
        Debug.Log("to main menu!");
        SceneManager.LoadSceneAsync("Menu");
    }
    public void OpenQuitDialog()
    {
        quitDialog.SetActive(true);
    }

    public void CloseQuitDialog()
    {
        quitDialog.SetActive(false);
    }

    public void QuitGame()
    {
        gameManager.QuitGame();
    }
}
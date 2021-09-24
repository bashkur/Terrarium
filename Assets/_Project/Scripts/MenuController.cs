using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject quitDialog;
    [SerializeField] private GameObject mainMenu;
    private GameManager gameManager;

    private void Awake()
    {
        quitDialog.SetActive(false);
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager.gameState == GameManager.GameState.MainMenu)
        {
            mainMenu.SetActive(true);
        }
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);
        gameManager.StartGame();
    }

    public void ShowCredits()
    {
        Debug.Log("Credits go here");
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
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject quitDialog;

    private void Awake()
    {
        quitDialog.SetActive(false);
    }
    
    public void StartGame()
    {
        SceneManager.LoadScene("BashSandbox");
    }

    public void ShowCredits()
    {
        Debug.Log("Credits go here");
    }

    public void QuitGame()
    {
        Debug.Log("quit game");
        GameManager.Instance.QuitGame();
    }

    public void OpenQuitDialog()
    {
        quitDialog.SetActive(true);
    }

    public void CloseQuitDialog()
    {
        quitDialog.SetActive(false);
    }
}

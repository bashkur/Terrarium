using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject blurPanel;
    [SerializeField] private GameObject quitDialog;

    private void Awake()
    {
        blurPanel.SetActive(false);
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
        Application.Quit();
    }

    public void OpenModal()
    {
        blurPanel.SetActive(true);
        quitDialog.SetActive(true);
    }

    public void CloseModal()
    {
        blurPanel.SetActive(false);
        quitDialog.SetActive(false);
    }
}

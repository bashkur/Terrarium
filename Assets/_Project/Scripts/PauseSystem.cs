using UnityEngine;

public class PauseSystem : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    // Start is called before the first frame update
    void Start()
    {
        pauseMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager.Instance.TogglePauseGame();
            pauseMenu.SetActive(GameManager.IsPaused);
        }
    }
}

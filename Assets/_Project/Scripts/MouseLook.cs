using System;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 200f;
    [SerializeField] private float lookAngleLimit = 45f;
    [SerializeField] private Transform player;

    private float currentXRotation = 0f;
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    private void Update()
    {
        if (gameManager.gameState == GameManager.GameState.Paused)
        {
            // do not process player movement when game is paused
            return;
        }
        
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        currentXRotation -= mouseY;
        currentXRotation = Mathf.Clamp(currentXRotation, -lookAngleLimit, lookAngleLimit);
        transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);
        player.Rotate(Vector3.up * mouseX);
    }
}

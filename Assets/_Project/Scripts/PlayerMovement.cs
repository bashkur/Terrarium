using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float walkSpeed = 5f;

    private GameManager gameManager;
    private CharacterController characterController;
    private Vector3 velocity;

    private void Start()
    {
        gameManager = GameManager.Instance;
        gameManager.gameState = GameManager.GameState.Running;
        
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (gameManager.gameState == GameManager.GameState.Paused)
        {
            // do not process player movement when game is paused
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            gameManager.UpdateScore(1);
        }
        
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
        characterController.Move(move * walkSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
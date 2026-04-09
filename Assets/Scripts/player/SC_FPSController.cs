using UnityEngine;

/// <summary>
/// SC_FPSController sinifi, birinci sahis hareket, bakis ve stamina akislarini yonetir.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SC_FPSController : MonoBehaviour
{
    public float walkingSpeed = 3.0f;
    public float runningSpeed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRecoveryRate = 5f;

    private float currentStamina;
    private bool isRunning = false;
    private bool isJumping = false;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private Animator animator;

    [HideInInspector] public bool canMove = true;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (!canMove)
        {
            return;
        }

        if (PauseMenuUI.IsInputLocked)
        {
            return;
        }

        HandleMovementInput();
        HandleCameraLook();
        HandleStamina();
    }

    private void HandleMovementInput()
    {
        isRunning = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0f;

        if (Input.GetButtonDown("Jump") && characterController.isGrounded && currentStamina > 0f)
        {
            isJumping = true;
        }

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        float curSpeedY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");
        float movementDirectionY = moveDirection.y;

        if (characterController.isGrounded)
        {
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            if (isJumping)
            {
                moveDirection.y = jumpSpeed;
                currentStamina -= 10f;
                currentStamina = Mathf.Max(currentStamina, 0f);
                isJumping = false;
            }
        }
        else
        {
            moveDirection.y = movementDirectionY - (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleCameraLook()
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
    }

    private void HandleStamina()
    {
        if (currentStamina <= 0f)
        {
            isRunning = false;
            isJumping = false;
        }

        if (isRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    public void freeze()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        canMove = false;
        playerCamera.enabled = false;
    }

    public void unfreeze()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        canMove = true;
        playerCamera.enabled = true;
    }
}

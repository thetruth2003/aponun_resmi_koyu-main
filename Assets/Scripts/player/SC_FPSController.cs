using System.Collections;
using UnityEngine;
using RengeGames.HealthBars;

[RequireComponent(typeof(CharacterController))]
public class SC_FPSController : MonoBehaviour
{
    // === Movement Settings ===
    public float walkingSpeed = 3.0f;
    public float runningSpeed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    // === Camera Settings ===
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    // === Stamina Settings ===
    public UltimateCircularHealthBar staminaBar;
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRecoveryRate = 5f;
    private float currentStamina;

    // === States ===
    private bool isRunning = false;
    private bool isJumping = false;

    // === Movement Control ===
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [HideInInspector] public bool canMove = true;

    private Animator animator;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!canMove) return;

        HandleMovementInput();
        HandleCameraLook();
        HandleStamina();
    }

    void HandleMovementInput()
    {
        // === Koşma kontrolü ===
        isRunning = Input.GetKey(KeyCode.LeftShift) && currentStamina > 0;

        // === Zıplama kontrolü ===
        if (Input.GetButtonDown("Jump") && characterController.isGrounded && currentStamina > 0)
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
                currentStamina = Mathf.Max(currentStamina, 0);
                isJumping = false;
            }
        }
        else
        {
            moveDirection.y = movementDirectionY - (gravity * Time.deltaTime);
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    void HandleCameraLook()
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    void HandleStamina()
    {
        if (currentStamina <= 0)
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

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);

        float removedSegments = (1 - currentStamina / maxStamina) * staminaBar.SegmentCount;
        staminaBar.SetRemovedSegments(removedSegments);
    }

    // === Freeze / Unfreeze (Mouse ve Kamera) ===
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

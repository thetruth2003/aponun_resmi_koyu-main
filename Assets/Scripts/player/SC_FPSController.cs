using System.Collections;
using System.Collections.Generic;
using RengeGames.HealthBars;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class SC_FPSController : MonoBehaviour
{
    // Hareket ve kamera ayarları
    public float walkingSpeed = 3.0f;  // Yürüme hızı
    public float runningSpeed = 6.0f;  // Koşma hızı
    public float jumpSpeed = 8.0f;     // Zıplama hızı
    public float gravity = 20.0f;      // Yerçekimi kuvveti
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    // Stamina Bar ve Ayarları
    public UltimateCircularHealthBar staminaBar; // Stamina bar referansı
    public float maxStamina = 100f;              // Maksimum stamina
    public float staminaDrainRate = 10f;          // Koşarken stamina kaybı oranı
    public float staminaRecoveryRate = 5f;        // Koşulmadığında stamina geri dolma oranı
    private float currentStamina;                 // Mevcut stamina
    public bool isRunning = false;                // Koşma durumu
    public bool isJumping = false;                // Zıplama durumu

    // Karakter ve hareket kontrolü
    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    Animator animator;

    void Start()
    {
        // Başlangıç ayarları
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;  // Başlangıçta stamina'nın tam olmasını sağla
    }

    void Update()
    {
        // Eğer hareket edilemiyorsa çık
        if (!canMove) return;

        // Koşma kontrolü: LeftShift tuşu ile koşma kontrolü
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0)  // Koşmak için LeftShift tuşu ve stamina'nın varlığı
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }

        // Zıplama kontrolü
        if (Input.GetButtonDown("Jump") && characterController.isGrounded && currentStamina > 0)
        {
            isJumping = true;
        }

        // İleri/geri hareket için yön hesapla
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Yönlere bağlı hareket hızı
        float curSpeedX = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical");
        float curSpeedY = (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal");

        // Yükseklik hareketini (zıplama) koru
        float movementDirectionY = moveDirection.y;

        // Yere temas ettiğinde hareket yönünü güncelle
        if (characterController.isGrounded)
        {
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            // Zıplama tuşuna basıldıysa yukarı doğru hız ver
            if (isJumping)
            {
                moveDirection.y = jumpSpeed;
                currentStamina -= 10f; // Zıplarken stamina kaybı (istediğiniz değeri ayarlayın)
                if (currentStamina < 0) currentStamina = 0;
                isJumping = false;
            }
        }
        else
        {
            // Havadayken yerçekimini uygula
            moveDirection.y = movementDirectionY - (gravity * Time.deltaTime);
        }

        // Karakteri hareket ettir
        characterController.Move(moveDirection * Time.deltaTime);

        // Speed'i hesapla
        float speed = new Vector3(characterController.velocity.x, 0, characterController.velocity.z).magnitude;

        // Player ve Kamera dönüşlerini kontrol et
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        // Stamina'yı yönet
        HandleStamina();
    }

    void HandleStamina()
    {
        if (currentStamina <= 0)
        {
            // Stamina bitince koşma ve zıplamayı engelle
            isRunning = false;   // Koşmayı engelle
            isJumping = false;   // Zıplamayı engelle
        }
        else
        {
            if (isRunning)
            {
                // Koşarken stamina'yı azalt
                currentStamina -= staminaDrainRate * Time.deltaTime;
                if (currentStamina < 0) currentStamina = 0;
            }
            else
            {
                // Koşulmadığında stamina geri dolsun
                currentStamina += staminaRecoveryRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        // Stamina barındaki RemovedSegments değerini güncelle
        float removedSegments = (1 - currentStamina / maxStamina) * staminaBar.SegmentCount;
        staminaBar.SetRemovedSegments(removedSegments);
    }
}

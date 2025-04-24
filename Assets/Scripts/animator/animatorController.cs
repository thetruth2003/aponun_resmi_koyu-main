using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animatorController : MonoBehaviour
{
    public float walkingSpeed = 3.0f;  // Y�r�me h�z�
    public float runningSpeed = 6.0f;  // Ko�ma h�z�

    private Animator animator;         // Animator referans�
    private CharacterController characterController;

    void Start()
    {
        animator = GetComponent<Animator>();  // Animator'u al
        characterController = GetComponent<CharacterController>();  // KarakterController'� al
    }

    void Update()
    {
        // Hareket parametrelerini al
        float vertical = Input.GetAxis("Vertical");  // �leri ve geri hareket
        bool isRunning = Input.GetKey(KeyCode.LeftShift); // Ko�ma durumunu kontrol et

        // Hareketi hesapla
        float currentSpeed = isRunning ? runningSpeed : walkingSpeed;

        // Yaln�zca W tu�una bas�ld���nda hareket etmesini sa�la
        if (vertical > 0)  // W tu�una bas�ld���nda
        {
            // Karakteri ileri hareket ettir
            Vector3 movement = transform.forward * currentSpeed * vertical * Time.deltaTime;
            characterController.Move(movement);

            // Animator'a hareket parametresi g�nder
            animator.SetFloat("Speed", currentSpeed);  // H�z parametresi ile animasyonu kontrol et

            // E�er ko�uyorsa animasyon ge�i�ini sa�la
            if (isRunning)
            {
                animator.SetBool("isRunning", true);  // Ko�ma animasyonu tetiklenecek
            }
            else
            {
                animator.SetBool("isRunning", false); // Ko�ma animasyonu duracak, y�r�me animasyonuna ge�ecek
            }
        }
        else
        {
            // Yava�la, durmaya ge�
            animator.SetFloat("Speed", 0);  // Animasyonu durdur
            animator.SetBool("isRunning", false); // Ko�ma animasyonunu durdur
        }
    }
}

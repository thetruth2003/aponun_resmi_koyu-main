using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animatorController : MonoBehaviour
{
    public float walkingSpeed = 3.0f;  // Yürüme hýzý
    public float runningSpeed = 6.0f;  // Koþma hýzý

    private Animator animator;         // Animator referansý
    private CharacterController characterController;

    void Start()
    {
        animator = GetComponent<Animator>();  // Animator'u al
        characterController = GetComponent<CharacterController>();  // KarakterController'ý al
    }

    void Update()
    {
        // Hareket parametrelerini al
        float vertical = Input.GetAxis("Vertical");  // Ýleri ve geri hareket
        bool isRunning = Input.GetKey(KeyCode.LeftShift); // Koþma durumunu kontrol et

        // Hareketi hesapla
        float currentSpeed = isRunning ? runningSpeed : walkingSpeed;

        // Yalnýzca W tuþuna basýldýðýnda hareket etmesini saðla
        if (vertical > 0)  // W tuþuna basýldýðýnda
        {
            // Karakteri ileri hareket ettir
            Vector3 movement = transform.forward * currentSpeed * vertical * Time.deltaTime;
            characterController.Move(movement);

            // Animator'a hareket parametresi gönder
            animator.SetFloat("Speed", currentSpeed);  // Hýz parametresi ile animasyonu kontrol et

            // Eðer koþuyorsa animasyon geçiþini saðla
            if (isRunning)
            {
                animator.SetBool("isRunning", true);  // Koþma animasyonu tetiklenecek
            }
            else
            {
                animator.SetBool("isRunning", false); // Koþma animasyonu duracak, yürüme animasyonuna geçecek
            }
        }
        else
        {
            // Yavaþla, durmaya geç
            animator.SetFloat("Speed", 0);  // Animasyonu durdur
            animator.SetBool("isRunning", false); // Koþma animasyonunu durdur
        }
    }
}

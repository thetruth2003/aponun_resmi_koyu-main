using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// animatorController sinifi, ilgili nesnenin kontrol ve davranis akislarini yonetir.
/// </summary>
public class animatorController : MonoBehaviour
{
    public float walkingSpeed = 3.0f;
    public float runningSpeed = 6.0f;

    private Animator animator;
    private CharacterController characterController;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? runningSpeed : walkingSpeed;

        if (vertical > 0)
        {
            Vector3 movement = transform.forward * currentSpeed * vertical * Time.deltaTime;
            characterController.Move(movement);

            animator.SetFloat("Speed", currentSpeed);

            if (isRunning)
            {
                animator.SetBool("isRunning", true);
            }
            else
            {
                animator.SetBool("isRunning", false);
            }
        }
        else
        {
            animator.SetFloat("Speed", 0);
            animator.SetBool("isRunning", false);
        }
    }
}

using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("Idle Otomatik Hareketler")]
    public bool enableIdleBehaviors = true;
    public float idleMinTime = 8f;
    public float idleMaxTime = 15f;

    private float idleTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        ResetIdleTimer();
    }

    private bool isPlayingIdleVariant = false;

void Update()
{
    if (enableIdleBehaviors)
    {
        if (!isPlayingIdleVariant)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
            {
                int random = Random.Range(1, 3); // 1: Stretch, 2: WipeSweat
                animator.SetInteger("IdleVariant", random);
                isPlayingIdleVariant = true;
                ResetIdleTimer();
            }
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (isPlayingIdleVariant && stateInfo.IsName("stretch") || stateInfo.IsName("swipe"))
        {
            if (stateInfo.normalizedTime >= 1f)
            {
                animator.SetInteger("IdleVariant", 0);
                isPlayingIdleVariant = false;
            }
        }
    }
}


    private void ResetIdleTimer()
    {
        idleTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    /// <summary>
    /// Etkileşim (balta sallama, tohum ekme vs.) animasyonunu tetikler
    /// </summary>
    public void PlayInteractAnimation()
    {
        animator.SetTrigger("Interact");
    }

    /// <summary>
    /// İstediğin animasyon tetikleyicisini elle oynatmak için
    /// </summary>
    public void PlayTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Bool animasyon parametresi kullanıyorsan
    /// </summary>
    public void SetBool(string name, bool state)
    {
        animator.SetBool(name, state);
    }

    /// <summary>
    /// Örnek: koşma, zıplama gibi hız parametreleri
    /// </summary>
    public void SetFloat(string name, float value)
    {
        animator.SetFloat(name, value);
    }
}

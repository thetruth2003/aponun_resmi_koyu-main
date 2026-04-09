using UnityEngine;

/// <summary>
/// AnimationController sinifi, ilgili nesnenin kontrol ve davranis akislarini yonetir.
/// </summary>
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
                int random = Random.Range(1, 3);
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
    /// Etkile≈üim (balta sallama, tohum ekme vs.) animasyonunu tetikler
    /// </summary>
    public void PlayInteractAnimation()
    {
    }

    /// <summary>
    /// ???∞stedi???üin animasyon tetikleyicisini elle oynatmak i√ßin
    /// </summary>
    public void PlayTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Bool animasyon parametresi kullan???±yorsan
    /// </summary>
    public void SetBool(string name, bool state)
    {
        animator.SetBool(name, state);
    }

    /// <summary>
    /// √ñrnek: ko≈üma, z???±plama gibi h???±z parametreleri
    /// </summary>
    public void SetFloat(string name, float value)
    {
        animator.SetFloat(name, value);
    }
}

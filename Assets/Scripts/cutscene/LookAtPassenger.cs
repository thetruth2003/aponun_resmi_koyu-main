using UnityEngine;

public class LookAtPassenger : MonoBehaviour
{
    public Transform cameraTransform;      // Kamera objesi
    public float lookAngle = 45f;          // Sağa kaç derece dönsün
    public float lookDuration = 2f;        // Bakış süresi
    public float interval = 5f;            // Kaç saniyede bir baksın
    public float rotationSpeed = 2f;       // Dönüş hızı

    private Quaternion defaultRotation;
    private Quaternion targetRotation;
    private bool isLooking = false;
    private float lookTimer = 0f;
    private float intervalTimer = 0f;

    void Start()
    {
        if (cameraTransform == null) cameraTransform = transform;
        defaultRotation = cameraTransform.localRotation;

        // Sabit açıyla hedef rotasyon hesapla (sadece Y ekseninde döner)
        Vector3 lookAngles = defaultRotation.eulerAngles;
        lookAngles.y += lookAngle;
        targetRotation = Quaternion.Euler(lookAngles);
    }

    void Update()
    {
        intervalTimer += Time.deltaTime;

        if (!isLooking && intervalTimer >= interval)
        {
            isLooking = true;
            lookTimer = 0f;
            intervalTimer = 0f;
        }

        if (isLooking)
        {
            lookTimer += Time.deltaTime;
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);

            if (lookTimer >= lookDuration)
            {
                isLooking = false;
            }
        }
        else
        {
            cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, defaultRotation, Time.deltaTime * rotationSpeed);
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    [Header("Araç Özellikleri")]
    public float motorPower = 1500f; // motor gücü
    public float maxSpeed = 50f;     // km/h
    public float turnSpeed = 30f;
    public float weight = 1000f;     // kg

    [Header("Yük Özellikleri")]
    public bool hasTrailer = false;
    public float trailerWeight = 0f;

    [Header("Tekerlekler")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform backLeftWheel;
    public Transform backRightWheel;
    public float wheelRadius = 0.35f;

    private Rigidbody rb;
    private float steerInput;
    private float accelInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = weight;
    }

    void Update()
    {
        // Girdi al
        steerInput = Input.GetAxis("Horizontal");
        accelInput = Input.GetAxis("Vertical");

        // Ön teker yönlerini değiştir (sadece görsel olarak)
        float steerAngle = steerInput * 30f;
        frontLeftWheel.localRotation = Quaternion.Euler(0, steerAngle, 0);
        frontRightWheel.localRotation = Quaternion.Euler(0, steerAngle, 0);
    }

    void FixedUpdate()
    {
        float effectiveWeight = weight + (hasTrailer ? trailerWeight : 0f);

        // Hız hesapla (m/s)
        float speed = rb.velocity.magnitude;

        // Motor kuvveti uygula
        if (speed * 3.6f < maxSpeed) // km/h sınırı
        {
            Vector3 force = transform.forward * accelInput * (motorPower / effectiveWeight);
            rb.AddForce(force, ForceMode.Force);
        }

        // Dönüş kuvveti uygula
        if (Mathf.Abs(steerInput) > 0.01f && speed > 0.1f)
        {
            float direction = Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1f : -1f;
            float turn = steerInput * turnSpeed * direction * Time.fixedDeltaTime;
            Quaternion turnRot = Quaternion.Euler(0, turn, 0);
            rb.MoveRotation(rb.rotation * turnRot);
        }

        RotateWheels();
    }

    void RotateWheels()
    {
        float rotationAmount = rb.velocity.magnitude / (2 * Mathf.PI * wheelRadius) * 360 * Time.fixedDeltaTime;
        float direction = Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1f : -1f;
        backLeftWheel.Rotate(Vector3.right, rotationAmount * direction);
        backRightWheel.Rotate(Vector3.right, rotationAmount * direction);
        frontLeftWheel.Rotate(Vector3.right, rotationAmount * direction);
        frontRightWheel.Rotate(Vector3.right, rotationAmount * direction);
    }
}

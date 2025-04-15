using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController1 : MonoBehaviour
{
    public enum VehicleType { Harvester, Excavator, Tractor, Wheelbarrow }
    public VehicleType vehicleType;

    public float motorForce = 3000f; // Motor gücü
    public float brakeForce = 1500f; // Frenleme gücü
    public float maxSpeed = 100f;    // Maksimum hız
    public float turnSpeed = 100f;   // Dönüş hızı

    // Tekerlekler için referanslar
    public Transform[] frontWheels; // Ön tekerlekler
    public Transform[] rearWheels;  // Arka tekerlekler

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = 2f;
    }

    void Update()
    {
        // Kullanıcıdan input al
        GetInput();
        HandleMovement();
        HandleSteering();

        // Tekerlek dönüşünü güncelle
        RotateWheels();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal"); // A ve D tuşları için
        verticalInput = Input.GetAxis("Vertical");     // W ve S tuşları için
    }

    private void HandleMovement()
    {
        currentSpeed = rb.velocity.magnitude;

        // Aracı ileri/geri hareket ettir
        if (verticalInput > 0 && currentSpeed < maxSpeed)
        {
            rb.AddForce(transform.forward * verticalInput * motorForce * Time.deltaTime);
        }
        else if (verticalInput < 0 && currentSpeed > 0)
        {
            rb.AddForce(transform.forward * verticalInput * brakeForce * Time.deltaTime);
        }
    }

    private void HandleSteering()
    {
        if (horizontalInput != 0)
        {
            float turnAmount = horizontalInput * turnSpeed * Time.deltaTime;

            // Ön tekerleklerin sağa/sola dönüşü
            foreach (Transform wheel in frontWheels)
            {
                wheel.Rotate(Vector3.up, turnAmount);
            }

            // Araç dönüşü
            transform.Rotate(0, turnAmount, 0);
        }
    }

    private void RotateWheels()
    {
        // Tekerleklerin dönüşü için her birini döndürüyoruz
        foreach (Transform wheel in frontWheels)
        {
            // Tekerleklerin dönüşünü hızla orantılı bir şekilde ayarlıyoruz
            wheel.Rotate(Vector3.right * currentSpeed * Time.deltaTime, Space.Self);
        }

        foreach (Transform wheel in rearWheels)
        {
            // Arka tekerleklerin de dönüşü
            wheel.Rotate(Vector3.right * currentSpeed * Time.deltaTime, Space.Self);
        }
    }
}

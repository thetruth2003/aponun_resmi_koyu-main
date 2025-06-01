// Gerçekçi Araç Fizik Sistemi (Motor Freni + Patinaj + Çekiş + Geri Vites + Takla Sorunu Çözümü)

using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class RealisticCarController : MonoBehaviour
{
    public enum Drivetrain { FWD, RWD, AWD }

    [Header("Motor Parametreleri")]
    public float engineDisplacement = 2000f;
    public float maxRPM = 7000f;
    public float idleRPM = 900f;
    public float currentRPM = 0f;
    [Range(-1f, 1f)] public float throttleInput = 0f;

    [Header("Güç Çıkışı")]
    public float engineTorque;
    public float enginePower;
    private float lastThrottle;

    [Header("Tork Eğrisi")]
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

    [Header("Direksiyon Kontrolü")]
    public float steerAngle = 30f;
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Sürüş Dinamiği")]
    public Drivetrain drivetrain = Drivetrain.FWD;
    public float motorBrakeForce = 3000f;

    [Header("UI")]
    public TextMeshProUGUI rpmText, torqueText, powerText, gearText, speedText, inputText;

    private Rigidbody rb;
    private float inputSteer;
    private int currentGear = 1;
    private bool reversing = false;
    private float[] gearRatios = { 2.8f, 2.0f, 1.4f, 1.0f, 0.8f };
    private float[] gearMaxSpeeds = { 40f, 70f, 110f, 150f, 200f };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1300f;
        rb.drag = 0.02f;
        rb.angularDrag = 2.5f;
        rb.centerOfMass = new Vector3(0f, -0.9f, 0f);
        FixWheelFriction();
        FixSuspension();
    }

    void Update()
    {
        // Giriş
        if (Input.GetKey(KeyCode.W))
            throttleInput = Mathf.MoveTowards(throttleInput, 1f, Time.deltaTime * 3f);
        else if (Input.GetKey(KeyCode.S))
            throttleInput = Mathf.MoveTowards(throttleInput, -1f, Time.deltaTime * 3f);
        else
            throttleInput = Mathf.MoveTowards(throttleInput, 0f, Time.deltaTime * 4f);

        inputSteer = Input.GetAxis("Horizontal");

        // Geri vites kontrol
        if (!reversing && throttleInput < -0.1f && rb.velocity.magnitude < 0.5f)
            reversing = true;
        if (reversing && throttleInput > 0.1f)
            reversing = false;

        UpdateUI();
    }

    void FixedUpdate()
    {
        float speed = rb.velocity.magnitude * 3.6f;
        HandleEngine(speed);
        ApplySteering();
        ApplyBraking();
        UpdateWheelVisuals();
    }
        void ApplyBraking()
    {
        float brakeForce = 0f;

        // Geri fren
        if (throttleInput < 0f && rb.velocity.magnitude > 1f && Vector3.Dot(rb.velocity, transform.forward) > 0f)
        {
            brakeForce = 3000f;
            ApplyBrakeTorque(brakeForce);
            return;
        }

        // El freni (çekişe göre)
        if (Input.GetKey(KeyCode.Space))
        {
            switch (drivetrain)
            {
                case Drivetrain.FWD:
                    rearLeftCollider.brakeTorque = 5000f;
                    rearRightCollider.brakeTorque = 5000f;
                    break;
                case Drivetrain.RWD:
                    frontLeftCollider.brakeTorque = 5000f;
                    frontRightCollider.brakeTorque = 5000f;
                    break;
                case Drivetrain.AWD:
                    ApplyBrakeTorque(5000f);
                    break;
            }
        }
        else
        {
            ApplyBrakeTorque(0f);
        }
    }

        void HandleEngine(float speed)
    {
        float gearTopSpeed = gearMaxSpeeds[Mathf.Clamp(currentGear - 1, 0, gearMaxSpeeds.Length - 1)];
        float gearRatio = gearRatios[Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1)];

        // 1. Motor devrini sadece hızla orantıla
        currentRPM = Mathf.Lerp(idleRPM, maxRPM, Mathf.Clamp01(speed / gearTopSpeed));

        // 2. Gaz basılıysa RPM artışı ve tork üretimi
        if (Mathf.Abs(throttleInput) > 0.05f)
        {
            float torquePercent = torqueCurve.Evaluate(currentRPM / maxRPM);
            engineTorque = (engineDisplacement / 10f) * torquePercent * Mathf.Abs(throttleInput) * 100f;
            enginePower = (engineTorque * currentRPM) / 7127f;

            float wheelTorque = engineTorque * gearRatio;
            float direction = Mathf.Sign(throttleInput);
            ApplyMotorTorque(wheelTorque * direction);
        }
        else
        {
            // 3. Gaz yoksa tork sıfır, sadece motor freni devrede
            engineTorque = 0f;
            enginePower = 0f;
            ApplyMotorTorque(0f);

            if (speed > 1f)
                ApplyBrakeTorque(motorBrakeForce);
            else
                ApplyBrakeTorque(0f);
        }

        // 4. Otomatik vites değişimi
        if (throttleInput > 0)
        {
            if (currentGear < gearRatios.Length && currentRPM >= 6000f && speed >= gearTopSpeed * 0.95f)
                currentGear++;
            else if (currentGear > 1 && currentRPM < 2000f && speed < gearMaxSpeeds[currentGear - 2] * 0.85f)
                currentGear--;
        }
    }


    void ApplyMotorTorque(float torque)
    {
        switch (drivetrain)
        {
            case Drivetrain.FWD:
                frontLeftCollider.motorTorque = torque * 0.5f;
                frontRightCollider.motorTorque = torque * 0.5f;
                rearLeftCollider.motorTorque = 0f;
                rearRightCollider.motorTorque = 0f;
                break;
            case Drivetrain.RWD:
                rearLeftCollider.motorTorque = torque * 0.5f;
                rearRightCollider.motorTorque = torque * 0.5f;
                frontLeftCollider.motorTorque = 0f;
                frontRightCollider.motorTorque = 0f;
                break;
            case Drivetrain.AWD:
                float split = torque * 0.25f;
                frontLeftCollider.motorTorque = split;
                frontRightCollider.motorTorque = split;
                rearLeftCollider.motorTorque = split;
                rearRightCollider.motorTorque = split;
                break;
        }
    }

    void ApplyBrakeTorque(float brake)
    {
        bool handbrake = Input.GetKey(KeyCode.Space);

        if (handbrake)
        {
            if (drivetrain == Drivetrain.FWD)
            {
                rearLeftCollider.brakeTorque = brake;
                rearRightCollider.brakeTorque = brake;
            }
            else if (drivetrain == Drivetrain.RWD)
            {
                frontLeftCollider.brakeTorque = brake;
                frontRightCollider.brakeTorque = brake;
            }
            else
            {
                ApplyAllBrakes(brake);
            }
        }
        else
        {
            ApplyAllBrakes(brake);
        }
    }

    void ApplyAllBrakes(float value)
    {
        frontLeftCollider.brakeTorque = value;
        frontRightCollider.brakeTorque = value;
        rearLeftCollider.brakeTorque = value;
        rearRightCollider.brakeTorque = value;
    }

    void ApplySteering()
    {
        float steer = inputSteer * steerAngle;
        frontLeftCollider.steerAngle = steer;
        frontRightCollider.steerAngle = steer;
    }

    void UpdateWheelVisuals()
    {
        UpdateWheelPose(frontLeftCollider, frontLeftTransform);
        UpdateWheelPose(frontRightCollider, frontRightTransform);
        UpdateWheelPose(rearLeftCollider, rearLeftTransform);
        UpdateWheelPose(rearRightCollider, rearRightTransform);
    }

    void UpdateWheelPose(WheelCollider col, Transform trans)
    {
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        trans.position = pos;
        trans.rotation = rot;
    }

    void UpdateUI()
    {
        if (rpmText) rpmText.text = "RPM: " + Mathf.RoundToInt(currentRPM);
        if (torqueText) torqueText.text = "Torque: " + engineTorque.ToString("F0") + " Nm";
        if (powerText) powerText.text = "Power: " + enginePower.ToString("F0") + " HP";
        if (gearText) gearText.text = reversing ? "Gear: R" : ("Gear: " + currentGear);
        if (speedText) speedText.text = "Speed: " + Mathf.RoundToInt(rb.velocity.magnitude * 3.6f) + " km/h";

        if (inputText)
        {
            string keys = "";
            if (Input.GetKey(KeyCode.W)) keys += "W ";
            if (Input.GetKey(KeyCode.S)) keys += "S ";
            if (Input.GetKey(KeyCode.A)) keys += "A ";
            if (Input.GetKey(KeyCode.D)) keys += "D ";
            if (Input.GetKey(KeyCode.Space)) keys += "SPACE ";
            inputText.text = "Input: " + keys;
        }
    }

    void FixWheelFriction()
    {
        foreach (var col in new[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider })
        {
            WheelFrictionCurve forward = col.forwardFriction;
            forward.stiffness = 2.5f;
            col.forwardFriction = forward;

            WheelFrictionCurve sideways = col.sidewaysFriction;
            sideways.stiffness = 2.5f;
            col.sidewaysFriction = sideways;
        }
    }

    void FixSuspension()
    {
        foreach (var col in new[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider })
        {
            JointSpring spring = col.suspensionSpring;
            spring.spring = 35000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.4f;
            col.suspensionSpring = spring;
            col.suspensionDistance = 0.2f;
        }
    }
}

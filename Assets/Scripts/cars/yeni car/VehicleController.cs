using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public VehicleConfig config;

    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    public EngineAudioController audioController;
    public TextMeshProUGUI rpmText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI speedText;

    private Rigidbody rb;
    private int currentGear = 0;
    private float currentRPM;
    private bool isShifting;
    private float shiftTimer;
    private float throttleInput;

    // === Stability params ===
    [Header("Stability Settings")]
    public Vector3 comOffset = new Vector3(0, -0.5f, 0); // COM alçalt
    public float antiRollForce = 5000f;                  // stabilizer bar gücü
    public float sideStability = 5f;                     // yan kayma bastırma gücü
    public float highSpeedSteerReducer = 120f;           // bu hızda steer yarıya düşer

    public float CurrentRPM => currentRPM;
    public int CurrentGear => currentGear;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.drag = config.drag;
        rb.centerOfMass += comOffset; // COM offset uygula
        SetupWheels();
    }

    private void Update()
    {
        HandleInput();
        HandleSteering();
        HandleEngine(Time.deltaTime);
        HandleBraking();
        UpdateWheelVisuals();
        UpdateUI();
    }

    private void FixedUpdate()
    {
        // Stabilize edici sistemler physics update’te
        ApplyAntiRoll(frontLeft, frontRight);
        ApplyAntiRoll(rearLeft, rearRight);
        StabilizeSideSlip();
    }

    private void HandleInput()
    {
        throttleInput = Input.GetAxis("Vertical");
    }

    private void HandleSteering()
    {
        float steerInput = Input.GetAxis("Horizontal");

        float speed = rb.velocity.magnitude * 3.6f; // km/h
        float steerLimiter = Mathf.Lerp(1f, 0.5f, speed / highSpeedSteerReducer); // hız arttıkça steer azalır

        float steerAngle = steerInput * config.maxSteerAngle * steerLimiter;

        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;
    }

    private void HandleEngine(float delta)
    {
        float vehicleSpeed = rb.velocity.magnitude * 3.6f; // km/h
        float gearRatio = config.gearRatios[currentGear];
        float wheelRPM = (rearLeft.rpm + rearRight.rpm) * 0.5f;

        // RPM hesapla
        currentRPM = Mathf.Lerp(currentRPM,
            Mathf.Clamp(wheelRPM * gearRatio * config.differentialRatio, config.idleRPM, config.maxRPM),
            Time.deltaTime * 5f);

        if (!isShifting)
        {
            if (currentGear == 0 && Mathf.Abs(throttleInput) > 0.1f)
            {
                StartCoroutine(ShiftGear(1));
            }
            else
            {
                float shiftSpeed = config.gearSpeedRanges[currentGear].shiftUpSpeed;
                if (currentRPM >= config.shiftUpRPM && vehicleSpeed > shiftSpeed && currentGear < config.gearRatios.Length - 1)
                {
                    StartCoroutine(ShiftGear(currentGear + 1));
                }
                else if (currentRPM < config.shiftDownRPM && currentGear > 1)
                {
                    StartCoroutine(ShiftGear(currentGear - 1));
                }
            }

            ApplyTorque(throttleInput);
        }
        else
        {
            shiftTimer += delta;
            if (shiftTimer >= config.shiftDuration)
                isShifting = false;
        }
    }

    private void ApplyTorque(float throttle)
    {
        float gearRatio = config.gearRatios[currentGear];
        float engineTorque = config.torque * config.torqueCurve.Evaluate(currentRPM / config.maxRPM) * throttle;
        float wheelTorque = engineTorque * gearRatio;

        bool applyFront = config.drivetrain == DrivetrainType.FWD || config.drivetrain == DrivetrainType.AWD;
        bool applyRear = config.drivetrain == DrivetrainType.RWD || config.drivetrain == DrivetrainType.AWD;

        if (applyFront)
        {
            frontLeft.motorTorque = wheelTorque / (applyRear ? 4f : 2f);
            frontRight.motorTorque = wheelTorque / (applyRear ? 4f : 2f);
        }
        if (applyRear)
        {
            rearLeft.motorTorque = wheelTorque / (applyFront ? 4f : 2f);
            rearRight.motorTorque = wheelTorque / (applyFront ? 4f : 2f);
        }

        // Motor freni
        if (Mathf.Abs(throttle) < 0.05f)
        {
            float motorBrake = 1500f;
            rearLeft.brakeTorque = motorBrake;
            rearRight.brakeTorque = motorBrake;
            frontLeft.brakeTorque = motorBrake;
            frontRight.brakeTorque = motorBrake;
        }
        else
        {
            rearLeft.brakeTorque = 0f;
            rearRight.brakeTorque = 0f;
            frontLeft.brakeTorque = 0f;
            frontRight.brakeTorque = 0f;
        }
    }

    private void HandleBraking()
    {
        float brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        float brakeForce = brakeInput * config.handbrakeForce;

        frontLeft.brakeTorque += brakeForce;
        frontRight.brakeTorque += brakeForce;
        rearLeft.brakeTorque += brakeForce;
        rearRight.brakeTorque += brakeForce;
    }

    private IEnumerator ShiftGear(int newGear)
    {
        isShifting = true;
        shiftTimer = 0f;
        audioController?.OnGearShift();
        currentGear = newGear;
        yield return null;
    }

    private void UpdateWheelVisuals()
    {
        UpdateWheelPose(frontLeft, frontLeftMesh);
        UpdateWheelPose(frontRight, frontRightMesh);
        UpdateWheelPose(rearLeft, rearLeftMesh);
        UpdateWheelPose(rearRight, rearRightMesh);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform mesh)
    {
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    private void SetupWheels()
    {
        SetupSingleWheel(frontLeft);
        SetupSingleWheel(frontRight);
        SetupSingleWheel(rearLeft);
        SetupSingleWheel(rearRight);
    }

    private void SetupSingleWheel(WheelCollider wc)
    {
        JointSpring spring = wc.suspensionSpring;
        spring.spring = config.suspensionSpring;
        spring.damper = config.suspensionDamper;
        wc.suspensionSpring = spring;
        wc.suspensionDistance = config.suspensionDistance;

        WheelFrictionCurve forward = wc.forwardFriction;
        forward.stiffness = config.forwardFrictionStiffness;
        wc.forwardFriction = forward;

        WheelFrictionCurve sideways = wc.sidewaysFriction;
        sideways.stiffness = config.sidewaysFrictionStiffness;
        wc.sidewaysFriction = sideways;
    }

    private void UpdateUI()
    {
        if (rpmText != null)
            rpmText.text = "RPM: " + Mathf.RoundToInt(currentRPM);

        if (gearText != null)
            gearText.text = "Gear: " + (currentGear == 0 ? "N" : (currentGear + 1).ToString());

        if (speedText != null)
        {
            float speed = rb.velocity.magnitude * 3.6f;
            speedText.text = "Speed: " + Mathf.RoundToInt(speed) + " km/h";
        }
    }

    // === STABILITY HELPERS ===

    void ApplyAntiRoll(WheelCollider left, WheelCollider right)
    {
        WheelHit hit;
        float travelL = 1.0f, travelR = 1.0f;

        bool groundedL = left.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-left.transform.InverseTransformPoint(hit.point).y - left.radius) / left.suspensionDistance;

        bool groundedR = right.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-right.transform.InverseTransformPoint(hit.point).y - right.radius) / right.suspensionDistance;

        float antiRoll = (travelL - travelR) * antiRollForce;

        if (groundedL)
            rb.AddForceAtPosition(left.transform.up * -antiRoll, left.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(right.transform.up * antiRoll, right.transform.position);
    }

    void StabilizeSideSlip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        localVel.x = Mathf.Lerp(localVel.x, 0f, sideStability * Time.fixedDeltaTime);
        rb.velocity = transform.TransformDirection(localVel);
    }
}

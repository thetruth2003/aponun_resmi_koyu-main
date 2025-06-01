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

    public float CurrentRPM => currentRPM;
    public int CurrentGear => currentGear;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.drag = config.drag;
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

    private void HandleInput()
    {
        throttleInput = Input.GetAxis("Vertical");
    }

    private void HandleSteering()
    {
        float steerInput = Input.GetAxis("Horizontal");
        float steerAngle = steerInput * config.maxSteerAngle;

        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;
    }

    private void HandleEngine(float delta)
    {
        float vehicleSpeed = rb.velocity.magnitude * 3.6f; // km/h
        float gearRatio = config.gearRatios[currentGear];
        float wheelRPM = (rearLeft.rpm + rearRight.rpm) * 0.5f;

        // RPM hesaplama - motor RPM'ini wheelRPM'den alıyoruz
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
}
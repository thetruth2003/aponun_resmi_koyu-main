using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Yeni arac kontrol sisteminde vites, tork, direksiyon ve denge akislarini yonetir.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public VehicleConfig config;

    [Header("Exit Point")]
    public Transform exitPoint;

    [Header("Wheels")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("UI / Audio")]
    public EngineAudioController audioController;
    public TextMeshProUGUI rpmText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI speedText;

    [Header("Instant Brake (Space)")]
    [Tooltip("Space ile verilecek yuksek fren torku")]
    public float instantBrakeTorque = 6000f;

    [Tooltip("Motor freni (gaz yokken hafif fren)")]
    public float engineBrakeTorque = 1500f;

    [Header("Reverse Gear (S)")]
    [Tooltip("S basiliyken otomatik geri vites")]
    public bool useAutoReverse = true;

    [Tooltip("Geri vites orani (pozitif yaz; iceride negatif uygulanir)")]
    public float reverseGearRatio = 3.0f;

    [Tooltip("Geri viteste hiz limiti (km/h)")]
    public float maxReverseSpeedKmh = 20f;

    [Tooltip("???°leri giderken S'ye bas???±nca Ã¶nce bu h???±za kadar frenle (km/h)")]
    public float stopForReverseKmh = 1.0f;

    private bool isReverse = false;

    [Header("Recovery (R ile)")]
    [Tooltip("Takla veya cakilma durumunda yukari kaldirma yuksekligi")]
    public float recoverLift = 1.5f;

    [Tooltip("R spamini onlemek icin bekleme (sn)")]
    public float recoverCooldown = 2.0f;

    [Tooltip("Guvenli poz kaydi icin minimum hiz (km/h)")]
    public float safeSpeedKmh = 5f;

    private float _lastRecoverTime;
    private Vector3 _lastSafePos;
    private Quaternion _lastSafeRot;
    private bool _hasSafePose;

    private Rigidbody rb;
    private int currentGear = 0;
    private float currentRPM;
    private bool isShifting;
    private float shiftTimer;
    private float throttleInput;

    [Header("Stability Settings")]
    public Vector3 comOffset = new Vector3(0, -0.5f, 0);
    public float antiRollForce = 5000f;
    public float sideStability = 5f;
    public float highSpeedSteerReducer = 120f;

    public float CurrentRPM => currentRPM;
    public int CurrentGear => currentGear;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = config.mass;
        rb.linearDamping = config.drag;
        rb.centerOfMass += comOffset;
        SetupWheels();
    }

    /// <summary>
    /// Input, motor, fren, teker gorselleri ve UI guncellemelerini tek karelik oyun akisinda toplar.
    /// </summary>
    private void Update()
    {
        HandleInput();

        if (Input.GetKeyDown(KeyCode.R))
            TryRecover();

        HandleSteering();
        HandleEngine(Time.deltaTime);
        HandleBrakingInstant();
        UpdateWheelVisuals();
        UpdateUI();

        SaveSafePoseTick();
    }

    private void FixedUpdate()
    {
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
        float speed = rb.linearVelocity.magnitude * 3.6f;
        float steerLimiter = Mathf.Lerp(1f, 0.5f, speed / highSpeedSteerReducer);
        float steerAngle = steerInput * config.maxSteerAngle * steerLimiter;

        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;
    }

    /// <summary>
    /// Aracin devrine, hizina ve ileri-geri durumuna gore uygun torku ve vites gecisini hesaplar.
    /// </summary>
    private void HandleEngine(float delta)
    {
        float vehicleSpeedKmh = rb.linearVelocity.magnitude * 3.6f;
        float vertical = Input.GetAxis("Vertical");

        if (useAutoReverse)
        {
            if (vertical < -0.1f)
            {
                if (!isReverse && vehicleSpeedKmh > stopForReverseKmh)
                {
                    frontLeft.motorTorque = frontRight.motorTorque = 0f;
                    rearLeft.motorTorque  = rearRight.motorTorque  = 0f;
                    SetBrakeAll(instantBrakeTorque);
                    return;
                }
                isReverse = true;
            }
            else if (vertical > 0.1f)
            {
                isReverse = false;
            }
        }

        if (!isReverse)
        {
            float gearRatio = config.gearRatios[currentGear];
            float wheelRPM = (rearLeft.rpm + rearRight.rpm) * 0.5f;

            currentRPM = Mathf.Lerp(
                currentRPM,
                Mathf.Clamp(wheelRPM * gearRatio * config.differentialRatio, config.idleRPM, config.maxRPM),
                Time.deltaTime * 5f
            );

            if (!isShifting)
            {
                if (currentGear == 0 && Mathf.Abs(throttleInput) > 0.1f)
                {
                    StartCoroutine(ShiftGear(1));
                }
                else
                {
                    float shiftSpeed = config.gearSpeedRanges[currentGear].shiftUpSpeed;
                    if (currentRPM >= config.shiftUpRPM && vehicleSpeedKmh > shiftSpeed && currentGear < config.gearRatios.Length - 1)
                        StartCoroutine(ShiftGear(currentGear + 1));
                    else if (currentRPM < config.shiftDownRPM && currentGear > 1)
                        StartCoroutine(ShiftGear(currentGear - 1));
                }

                ApplyTorqueForward(throttleInput);
            }
            else
            {
                shiftTimer += delta;
                if (shiftTimer >= config.shiftDuration)
                    isShifting = false;
            }
        }
        else
        {
            ApplyTorqueReverse(vertical);
            currentRPM = Mathf.Lerp(currentRPM, Mathf.Clamp(currentRPM, config.idleRPM, config.maxRPM), Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Vites degisimini kisa bir gecikme ile gerceklestirip ses tetigini de ayni anda calistirir.
    /// </summary>
    private IEnumerator ShiftGear(int newGear)
    {
        isShifting = true;
        shiftTimer = 0f;
        audioController?.OnGearShift();
        currentGear = newGear;
        yield return null;
    }

    /// <summary>
    /// Ileri viteste secili cekis tipine gore tekerlere motor torkunu dagitir.
    /// </summary>
    private void ApplyTorqueForward(float throttle)
    {
        float gearRatio = config.gearRatios[currentGear];
        float engineTorque = config.torque * config.torqueCurve.Evaluate(Mathf.Clamp01(currentRPM / config.maxRPM)) * throttle;
        float wheelTorque = engineTorque * gearRatio;

        bool applyFront = config.drivetrain == DrivetrainType.FWD || config.drivetrain == DrivetrainType.AWD;
        bool applyRear  = config.drivetrain == DrivetrainType.RWD || config.drivetrain == DrivetrainType.AWD;

        if (Mathf.Abs(throttle) > 0.05f) SetBrakeAll(0f);
        else SetBrakeAll(engineBrakeTorque);

        if (applyFront)
        {
            frontLeft.motorTorque  = wheelTorque / (applyRear ? 4f : 2f);
            frontRight.motorTorque = wheelTorque / (applyRear ? 4f : 2f);
        }
        if (applyRear)
        {
            rearLeft.motorTorque  = wheelTorque / (applyFront ? 4f : 2f);
            rearRight.motorTorque = wheelTorque / (applyFront ? 4f : 2f);
        }
    }

    /// <summary>
    /// Geri viteste hiz limitini koruyarak araci kontrollu sekilde geriye iter.
    /// </summary>
    private void ApplyTorqueReverse(float vertical)
    {
        float input = Mathf.Clamp01(-vertical);

        float speedKmh = rb.linearVelocity.magnitude * 3.6f;
        if (speedKmh > maxReverseSpeedKmh)
        {
            frontLeft.motorTorque = frontRight.motorTorque = 0f;
            rearLeft .motorTorque = rearRight .motorTorque = 0f;
            SetBrakeAll(engineBrakeTorque);
            return;
        }

        float gearRatio = -Mathf.Abs(reverseGearRatio);
        float engineTorque = config.torque * config.torqueCurve.Evaluate(Mathf.Clamp01(currentRPM / config.maxRPM)) * input;
        float wheelTorque = engineTorque * gearRatio;

        bool applyFront = config.drivetrain == DrivetrainType.FWD || config.drivetrain == DrivetrainType.AWD;
        bool applyRear  = config.drivetrain == DrivetrainType.RWD || config.drivetrain == DrivetrainType.AWD;

        if (input > 0.05f) SetBrakeAll(0f);
        else SetBrakeAll(engineBrakeTorque);

        if (applyFront)
        {
            frontLeft.motorTorque  = wheelTorque / (applyRear ? 4f : 2f);
            frontRight.motorTorque = wheelTorque / (applyRear ? 4f : 2f);
        }
        if (applyRear)
        {
            rearLeft.motorTorque  = wheelTorque / (applyFront ? 4f : 2f);
            rearRight.motorTorque = wheelTorque / (applyFront ? 4f : 2f);
        }
    }

    private void HandleBrakingInstant()
    {
        bool spaceBrake = Input.GetKey(KeyCode.Space);

        if (spaceBrake)
        {
            frontLeft.motorTorque = 0f;
            frontRight.motorTorque = 0f;
            rearLeft.motorTorque = 0f;
            rearRight.motorTorque = 0f;

            SetBrakeAll(instantBrakeTorque);
        }
    }

    private void SetBrakeAll(float t)
    {
        frontLeft.brakeTorque = t;
        frontRight.brakeTorque = t;
        rearLeft.brakeTorque = t;
        rearRight.brakeTorque = t;
    }

    /// <summary>
    /// Teker collider pozlarini mesh kemiklerine tasiyarak gorsel jant hareketini guncel tutar.
    /// </summary>
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
        if (mesh)
        {
            mesh.position = pos;
            mesh.rotation = rot;
        }
    }

    /// <summary>
    /// Devir, vites ve hiz bilgisini ekrana anlik olarak yazar.
    /// </summary>
    private void UpdateUI()
    {
        if (rpmText != null)
            rpmText.text = "RPM: " + Mathf.RoundToInt(currentRPM);

        if (gearText != null)
        {
            if (isReverse) gearText.text = "Gear: R";
            else gearText.text = "Gear: " + (currentGear == 0 ? "N" : (currentGear + 1).ToString());
        }

        if (speedText != null)
        {
            float speed = rb.linearVelocity.magnitude * 3.6f;
            speedText.text = "Speed: " + Mathf.RoundToInt(speed) + " km/h";
        }
    }

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
            rb.AddForceAtPosition(left.transform.up * -antiRoll, left.transform.position, ForceMode.Force);
        if (groundedR)
            rb.AddForceAtPosition(right.transform.up * antiRoll, right.transform.position, ForceMode.Force);
    }

    void StabilizeSideSlip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x = Mathf.Lerp(localVel.x, 0f, sideStability * Time.fixedDeltaTime);
        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    /// <summary>
    /// Arac ters kaldiysa son guvenli poza veya mevcut konuma yakin bir noktaya toparlanma uygular.
    /// </summary>
    private void TryRecover()
    {
        if (Time.time < _lastRecoverTime + recoverCooldown) return;
        _lastRecoverTime = Time.time;

        if (_hasSafePose)
        {
            TeleportTo(_lastSafePos + Vector3.up * recoverLift, Quaternion.Euler(0f, _lastSafeRot.eulerAngles.y, 0f));
            return;
        }

        Vector3 pos = transform.position + Vector3.up * recoverLift;
        Quaternion upright = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        TeleportTo(pos, upright);
    }

    private void TeleportTo(Vector3 pos, Quaternion rot)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(pos, rot);
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Arac durumu guvenliyken geri donus noktasi olarak kullanilacak son pozu hafizada tutar.
    /// </summary>
    private void SaveSafePoseTick()
    {
        float speed = rb.linearVelocity.magnitude * 3.6f;
        Vector3 up = transform.up;

        bool uprightEnough = Vector3.Dot(up, Vector3.up) > 0.7f;
        if (uprightEnough && speed > safeSpeedKmh)
        {
            _lastSafePos = transform.position;
            _lastSafeRot = transform.rotation;
            _hasSafePose = true;
        }
    }

    /// <summary>
    /// Dort tekerin suspansiyon, surtunme ve kutle ayarlarini config verisinden kurar.
    /// </summary>
    private void SetupWheels()
    {
        SetupSingleWheel(frontLeft);
        SetupSingleWheel(frontRight);
        SetupSingleWheel(rearLeft);
        SetupSingleWheel(rearRight);
    }

    private void SetupSingleWheel(WheelCollider wc)
    {
        if (!wc) return;

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
}

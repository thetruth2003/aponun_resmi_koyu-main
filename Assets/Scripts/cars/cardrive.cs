using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class cardrive : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;

    [Header("Wheel Meshes (optional)")]
    public Transform frontLeftMesh, frontRightMesh, rearLeftMesh, rearRightMesh;

    [Header("Steering & Brakes")]
    public float maxSteerAngle     = 28f;
    public float frontBrakeTorque  = 3200f;
    public float rearBrakeTorque   = 2400f;
    public float handbrakeTorque   = 6500f;
public enum DriveAxle { Front, Rear, All }

[Header("Drive Type")]
public DriveAxle driveAxle = DriveAxle.Rear;


    [Tooltip("İleri azami hız (km/h)")]
    public float maxForwardKPH = 160f;
    [Tooltip("Geri azami hız (km/h)")]
    public float maxReverseKPH = 35f;

    [Tooltip("Tekerleğe uygulanan motor torku (Nm)")]
    public float wheelMotorTorque = 1200f;

    [Tooltip("Gazı bırakınca hafif yavaşlama için ek fren (0 = kapalı)")]
    public float coastBrake = 800f;

    [Header("Stability / Grip")]
    [Tooltip("Yanal kaymayı sönümlemek için Rigidbody hızını yerelde X ekseninde sönümleme")]
    public float lateralGrip = 6f; // 0 = kapalı, 8-12 daha keskin yol tutuş

    [Header("Center of Mass Offset")]
    public Vector3 comOffset = new Vector3(0f, -0.3f, 0f);

    [Header("Resistances (basit)")]
    public float airDragCoefficient = 0.30f;
    public float frontalArea = 2.2f;
    public float airDensity = 1.225f;
    public float rollingResistance = 0.015f;
    public float carMass = 1200f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += comOffset;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        // ---- Input ----
        float vAxis   = Input.GetAxisRaw("Vertical");   // W/S
        float hAxis   = Input.GetAxisRaw("Horizontal"); // A/D
        bool handbrake = Input.GetKey(KeyCode.Space);

        // ---- Steering ----
        float steerAngle = hAxis * maxSteerAngle;
        if (frontLeft)  frontLeft.steerAngle  = steerAngle;
        if (frontRight) frontRight.steerAngle = steerAngle;

        // ---- Hız / yön tespiti ----
        float speedMS  = rb.velocity.magnitude;
        float speedKPH = speedMS * 3.6f;
        float signedFwdMS = Vector3.Dot(rb.velocity, transform.forward); // + ileri, - geri

        // ---- İleri mi geri mi gitmek istiyoruz? ----
        bool wantReverse = vAxis < -0.1f;
        bool wantForward = vAxis >  0.1f;

        // Hız limiti (aynı yönde gaz verilirken)
        float dirLimitKPH = wantReverse ? maxReverseKPH : maxForwardKPH;
        bool beyondLimit = (Mathf.Sign(signedFwdMS) == (wantReverse ? -1f : 1f))
                           && (Mathf.Abs(signedFwdMS) * 3.6f > dirLimitKPH);

        // ---- Motor torku hesapla (vites yok) ----
        float motor = 0f;

        if (wantForward)
        {
            // Geri gidiyorsa önce frenle, hız çok küçükse ileri tork ver
            if (signedFwdMS < -0.5f) {
                motor = 0f; // frenleyeceğiz
            } else if (!beyondLimit) {
                motor = +wheelMotorTorque;
            }
        }
        else if (wantReverse)
        {
            // İleri gidiyorsa önce frenle, hız çok küçükse geri tork ver
            if (signedFwdMS > 0.5f) {
                motor = 0f; // fren
            } else if (!beyondLimit) {
                motor = -wheelMotorTorque;
            }
        }
        // vAxis ≈ 0 ise motor = 0 (coast)

        // ---- Fren kuvvetleri ----
        float footF = 0f, footR = 0f;

        // Zıt yöne basılıyorsa ayak freni
        if ((wantForward && signedFwdMS < -0.5f) || (wantReverse && signedFwdMS > 0.5f))
        {
            footF = frontBrakeTorque;
            footR = rearBrakeTorque;
        }
        // Gaz yoksa coast freni
        else if (Mathf.Abs(vAxis) < 0.1f && coastBrake > 0f && speedKPH > 1f)
        {
            footF = coastBrake * 0.6f;
            footR = coastBrake * 0.4f;
        }

        // El freni (arka tekerlere eklenir)
        float hb = handbrake ? handbrakeTorque : 0f;

        // ---- Tork & frenleri tekerlere uygula ----
        ApplyWheel(frontLeft,  MotorToWheel(motor, true),  Mathf.Max(footF, 0f), hb, true);
        ApplyWheel(frontRight, MotorToWheel(motor, true),  Mathf.Max(footF, 0f), hb, true);
        ApplyWheel(rearLeft,   MotorToWheel(motor, false), Mathf.Max(footR, 0f), hb, false);
        ApplyWheel(rearRight,  MotorToWheel(motor, false), Mathf.Max(footR, 0f), hb, false);

        // ---- Görsel mesh eşitle ----
        UpdateWheelVisual(frontLeft,  frontLeftMesh);
        UpdateWheelVisual(frontRight, frontRightMesh);
        UpdateWheelVisual(rearLeft,   rearLeftMesh);
        UpdateWheelVisual(rearRight,  rearRightMesh);

        // ---- Basit yol tutuş / yanal kayma sönümleme ----
        if (lateralGrip > 0f && speedMS > 0.1f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
            localVel.x = Mathf.Lerp(localVel.x, 0f, lateralGrip * Time.fixedDeltaTime);
            rb.velocity = transform.TransformDirection(localVel);
        }

        // ---- Dirençler ----
        ApplyResistances();
    }

    float MotorToWheel(float motorNm, bool front)
    {
        switch (driveAxle)
        {
            case DriveAxle.Front: return front ? motorNm * 0.5f : 0f;          // iki ön teker paylaşır
            case DriveAxle.Rear:  return front ? 0f : motorNm * 0.5f;          // iki arka teker paylaşır
            case DriveAxle.All:   return motorNm * 0.25f;                       // dört teker eşit paylaşır
            default: return 0f;
        }
    }

    void ApplyWheel(WheelCollider wc, float motorNm, float footBrake, float handbrake, bool isFront)
    {
        if (!wc) return;
        wc.motorTorque = motorNm;
        float hb = (!isFront) ? handbrake : 0f;
        wc.brakeTorque = Mathf.Max(footBrake, hb);
    }

    void UpdateWheelVisual(WheelCollider col, Transform mesh)
    {
        if (!mesh || !col) return;
        col.GetWorldPose(out var pos, out var rot);
        mesh.SetPositionAndRotation(pos, rot);
    }

    void ApplyResistances()
    {
        var v = rb.velocity;
        float speed = v.magnitude;
        if (speed < 1e-3f) return;
        Vector3 dir = v / speed;

        // Hava direnci ~ v^2
        float drag = 0.5f * airDensity * airDragCoefficient * frontalArea * speed * speed;
        rb.AddForce(-dir * drag);

        // Yuvarlanma direnci ~ sabit
        float rr = rollingResistance * carMass * 9.81f;
        rb.AddForce(-dir * rr);
    }
}

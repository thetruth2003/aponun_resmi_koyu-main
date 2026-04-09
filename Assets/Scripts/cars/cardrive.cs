using UnityEngine;

/// <summary>
/// cardrive sinifi, arac sistemindeki ilgili davranisi yonetir.
/// </summary>
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
/// <summary>
/// DriveAxle sinifi, arac sistemindeki ilgili davranisi yonetir.
/// </summary>
public enum DriveAxle { Front, Rear, All }

[Header("Drive Type")]
public DriveAxle driveAxle = DriveAxle.Rear;

    [Tooltip("???∞leri azami h???±z (km/h)")]
    public float maxForwardKPH = 160f;
    [Tooltip("Geri azami h???±z (km/h)")]
    public float maxReverseKPH = 35f;

    [Tooltip("Tekerle???üe uygulanan motor torku (Nm)")]
    public float wheelMotorTorque = 1200f;

    [Tooltip("Gaz???± b???±rak???±nca hafif yava≈ülama i√ßin ek fren (0 = kapal???±)")]
    public float coastBrake = 800f;

    [Header("Stability / Grip")]
    [Tooltip("Yanal kaymay???± s√∂n√ºmlemek i√ßin Rigidbody h???±z???±n???± yerelde X ekseninde s√∂n√ºmleme")]
    public float lateralGrip = 6f;

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
        float vAxis   = Input.GetAxisRaw("Vertical");
        float hAxis   = Input.GetAxisRaw("Horizontal");
        bool handbrake = Input.GetKey(KeyCode.Space);

        float steerAngle = hAxis * maxSteerAngle;
        if (frontLeft)  frontLeft.steerAngle  = steerAngle;
        if (frontRight) frontRight.steerAngle = steerAngle;

        float speedMS  = rb.linearVelocity.magnitude;
        float speedKPH = speedMS * 3.6f;
        float signedFwdMS = Vector3.Dot(rb.linearVelocity, transform.forward);

        bool wantReverse = vAxis < -0.1f;
        bool wantForward = vAxis >  0.1f;

        float dirLimitKPH = wantReverse ? maxReverseKPH : maxForwardKPH;
        bool beyondLimit = (Mathf.Sign(signedFwdMS) == (wantReverse ? -1f : 1f))
                           && (Mathf.Abs(signedFwdMS) * 3.6f > dirLimitKPH);

        float motor = 0f;

        if (wantForward)
        {
            if (signedFwdMS < -0.5f) {
                motor = 0f;
            } else if (!beyondLimit) {
                motor = +wheelMotorTorque;
            }
        }
        else if (wantReverse)
        {
            if (signedFwdMS > 0.5f) {
                motor = 0f;
            } else if (!beyondLimit) {
                motor = -wheelMotorTorque;
            }
        }

        float footF = 0f, footR = 0f;

        if ((wantForward && signedFwdMS < -0.5f) || (wantReverse && signedFwdMS > 0.5f))
        {
            footF = frontBrakeTorque;
            footR = rearBrakeTorque;
        }
        else if (Mathf.Abs(vAxis) < 0.1f && coastBrake > 0f && speedKPH > 1f)
        {
            footF = coastBrake * 0.6f;
            footR = coastBrake * 0.4f;
        }

        float hb = handbrake ? handbrakeTorque : 0f;

        ApplyWheel(frontLeft,  MotorToWheel(motor, true),  Mathf.Max(footF, 0f), hb, true);
        ApplyWheel(frontRight, MotorToWheel(motor, true),  Mathf.Max(footF, 0f), hb, true);
        ApplyWheel(rearLeft,   MotorToWheel(motor, false), Mathf.Max(footR, 0f), hb, false);
        ApplyWheel(rearRight,  MotorToWheel(motor, false), Mathf.Max(footR, 0f), hb, false);

        UpdateWheelVisual(frontLeft,  frontLeftMesh);
        UpdateWheelVisual(frontRight, frontRightMesh);
        UpdateWheelVisual(rearLeft,   rearLeftMesh);
        UpdateWheelVisual(rearRight,  rearRightMesh);

        if (lateralGrip > 0f && speedMS > 0.1f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            localVel.x = Mathf.Lerp(localVel.x, 0f, lateralGrip * Time.fixedDeltaTime);
            rb.linearVelocity = transform.TransformDirection(localVel);
        }

        ApplyResistances();
    }

    float MotorToWheel(float motorNm, bool front)
    {
        switch (driveAxle)
        {
            case DriveAxle.Front: return front ? motorNm * 0.5f : 0f;
            case DriveAxle.Rear:  return front ? 0f : motorNm * 0.5f;
            case DriveAxle.All:   return motorNm * 0.25f;
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
        var v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 1e-3f) return;
        Vector3 dir = v / speed;

        float drag = 0.5f * airDensity * airDragCoefficient * frontalArea * speed * speed;
        rb.AddForce(-dir * drag);

        float rr = rollingResistance * carMass * 9.81f;
        rb.AddForce(-dir * rr);
    }
}

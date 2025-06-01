using UnityEngine;

[CreateAssetMenu(menuName = "Vehicle/Config")]
public class VehicleConfig : ScriptableObject
{
    [Header("Physics")]
    public float mass = 1300f;
    public float drag = 0.05f;

    [Header("Motor")]
    public float torque = 50000f; // Motor gücü (bunu artırırsan hızlanma artar)
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 0.3f, 1, 1); // RPM eğrisi
    public float maxRPM = 7000f;
    public float idleRPM = 900f;

    [Header("Gearing")]
    public float[] gearRatios = { 3.2f, 2.3f, 1.6f, 1.2f, 1.0f, 0.85f };
    public GearSpeedRange[] gearSpeedRanges; // Inspector'dan ayarla
    public float shiftUpRPM = 6200f;
    public float shiftDownRPM = 2500f;
    public float forceShiftRPM = 6800f;
    public float shiftDuration = 0.6f;
    public float differentialRatio = 3.42f; // Default value, adjust as needed

    [Header("Steering")]
    public float maxSteerAngle = 35f;

    [Header("Brakes")]
    public float handbrakeForce = 3000f;

    [Header("Suspension")]
    public float suspensionSpring = 35000f;
    public float suspensionDamper = 4500f;
    public float suspensionDistance = 0.2f;

    [Header("Wheel Friction")]
    public float forwardFrictionStiffness = 1.1f;
    public float sidewaysFrictionStiffness = 1.4f;

    [Header("Drivetrain")]
    public DrivetrainType drivetrain = DrivetrainType.RWD;
}

[System.Serializable]
public class GearSpeedRange
{
    public float maxSpeed = 40f;        // Max km/h for gear
    public float shiftUpSpeed = 35f;    // Speed to shift up
}

public enum DrivetrainType
{
    FWD, RWD, AWD
}

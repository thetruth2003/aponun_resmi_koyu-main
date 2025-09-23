using UnityEngine;

[CreateAssetMenu(menuName = "Vehicle/Config")]
public class VehicleConfig : ScriptableObject
{
    public float mass = 1200f;
    public float drag = 0.02f;
    public float[] gearRatios = { 2.5f, 1.8f, 1.3f, 1.0f, 0.8f };
    public float differentialRatio = 3.42f;
    public float idleRPM = 800f;
    public float maxRPM = 7000f;
    public float torque = 400f;
    public AnimationCurve torqueCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public float maxSteerAngle = 30f;
    public float shiftUpRPM = 6500f;
    public float shiftDownRPM = 1500f;
    public float shiftDuration = 0.3f;
    public GearSpeedRange[] gearSpeedRanges;
    public float handbrakeForce = 5000f;
    public float suspensionSpring = 35000f;
    public float suspensionDamper = 4500f;
    public float suspensionDistance = 0.2f;
    public float forwardFrictionStiffness = 1.5f;
    public float sidewaysFrictionStiffness = 2.0f;
    public DrivetrainType drivetrain = DrivetrainType.RWD;
}

[System.Serializable]
public struct GearSpeedRange
{
    public float shiftUpSpeed;
    public float shiftDownSpeed;
}

public enum DrivetrainType { FWD, RWD, AWD }
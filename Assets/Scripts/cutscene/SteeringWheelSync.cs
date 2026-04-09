using UnityEngine;

/// <summary>
/// SteeringWheelSync sinifi, cutscene akislarinda kullanilan ilgili davranisi yonetir.
/// </summary>
public class SteeringWheelSync : MonoBehaviour
{
    public Transform steeringVisual;
    public float turnAngle = 30f;

    void Update()
    {
        float steer = Mathf.Sin(Time.time * 2f) * turnAngle;
        steeringVisual.localRotation = Quaternion.Euler(0, 0, steer);
    }
}

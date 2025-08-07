using UnityEngine;

public class SteeringWheelSync : MonoBehaviour
{
    public Transform steeringVisual; // görsel direksiyon
    public float turnAngle = 30f;    // max dönme açısı

    void Update()
    {
        // NPC el animasyonu sabit, bu da aynı şekilde "oynuyormuş gibi" sağa sola dönsün
        float steer = Mathf.Sin(Time.time * 2f) * turnAngle;
        steeringVisual.localRotation = Quaternion.Euler(0, 0, steer);
    }
}

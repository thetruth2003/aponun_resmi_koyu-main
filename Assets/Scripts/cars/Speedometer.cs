using UnityEngine;
using TMPro;

/// <summary>
/// Speedometer sinifi, arac sistemindeki ilgili davranisi yonetir.
/// </summary>
public class Speedometer : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public Rigidbody activeCarRb;
    public Transform needleTransform;
    public float maxNeedleRotation = -90f;
    public float minNeedleRotation = 90f;

    void Update()
    {
        if (StateManger.Instance.car != null)
        {
            if (activeCarRb == null || activeCarRb.gameObject != StateManger.Instance.car)
            {
                activeCarRb = StateManger.Instance.car.GetComponent<Rigidbody>();
            }

            {
                float speed = activeCarRb.linearVelocity.magnitude * 3.6f;
                speedText.text = Mathf.RoundToInt(speed).ToString() + " km/h";

                float needleRotation = Mathf.Lerp(minNeedleRotation, maxNeedleRotation, speed / 200f);
                needleTransform.localRotation = Quaternion.Euler(0, 0, needleRotation);
            }
        }
        else
        {
            speedText.text = "0 km/h";
            if (needleTransform != null)
                needleTransform.localRotation = Quaternion.Euler(0, 0, minNeedleRotation);
            activeCarRb = null;
        }

    }
}

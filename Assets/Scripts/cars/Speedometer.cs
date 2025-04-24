using UnityEngine;
using TMPro; // Eğer TextMeshPro kullanıyorsanız bunu ekleyin

public class Speedometer : MonoBehaviour
{
    public TextMeshProUGUI speedText; // Hız göstergesi için TextMeshPro referansı
    public Rigidbody activeCarRb;   // Aktif aracın Rigidbody referansı
    public Transform needleTransform; // İğnenin Transform'u
    public float maxNeedleRotation = -90f; // Maksimum dönüş açısı (en yüksek hız)
    public float minNeedleRotation = 90f;  // Minimum dönüş açısı (durma)

    void Update()
    {
        // Eğer aktif bir araç varsa, hızını göster
        if (StateManger.Instance.car != null)
        {
            // Aktif aracın Rigidbody'sini kontrol et
            if (activeCarRb == null || activeCarRb.gameObject != StateManger.Instance.car)
            {
                activeCarRb = StateManger.Instance.car.GetComponent<Rigidbody>();
            }

            {
                // Hızı al ve UI'yi güncelle
                float speed = activeCarRb.velocity.magnitude * 3.6f; // m/s -> km/h dönüşümü
                speedText.text = Mathf.RoundToInt(speed).ToString() + " km/h";

                // İğnenin açısını hesapla
                float needleRotation = Mathf.Lerp(minNeedleRotation, maxNeedleRotation, speed / 200f); // 200 km/h varsayılan maksimum hız
                needleTransform.localRotation = Quaternion.Euler(0, 0, needleRotation);
            }
        }
        else
        {
            // Araç yoksa sıfırla
            speedText.text = "0 km/h";
            if (needleTransform != null)
                needleTransform.localRotation = Quaternion.Euler(0, 0, minNeedleRotation);
            activeCarRb = null;
        }

    }
}

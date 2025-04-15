using UnityEngine;
using System.Collections; // IEnumerator kullanabilmek için gerekli namespace

public class TreeFall : MonoBehaviour
{
    private Vector3 originalPosition;  // İlk pozisyonu saklar
    public float shakeAmount = 0.015f;  // Sallanma miktarı
    public bool isFalling = false;
    public float fallForce = 1.5f;  // Ağaç için eklenen kuvvet
    private int hitCount = 0;  // Vuruş sayacı
    private bool hasFallen = false;  // Ağacın devrilip devrilmediğini kontrol etmek için
    public GameObject odunsacma;

    private void Start()
    {

    }
    // Bu metodu child collider yere temas ettiğinde çağıracağız

    // Sallanma ve devrilme işlemi
    public IEnumerator ShakeAndFall()
    {
        hitCount++;  // Vuruş sayacını bir arttır

        // Her vuruşta sallanma yapılır
        originalPosition = transform.position;  // Ağaç pozisyonunu kaydet
        float shakeDuration = 0.2f;  // Sallanma süresi
        float shakeTimer = 0.0f;

        // Ağaç hafifçe sallanacak
        while (shakeTimer < shakeDuration)
        {
            shakeTimer += Time.deltaTime;
            transform.position = originalPosition + Random.insideUnitSphere * shakeAmount;  // Sallanma
            yield return null;
        }

        // Sallandıktan sonra pozisyonu sıfırla
        transform.position = originalPosition;

        // 5 vuruştan sonra devrilme işlemi başlat
        if (hitCount >= 5 && !isFalling)
        {
            isFalling = true;

            // Ağaç devrilmesi için bir kuvvet uygula
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;  // Fizikleri etkinleştir
                rb.useGravity = true;  // Yerçekimi etkisini aç
                Vector3 fallDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;  // Rastgele bir devrilme yönü
                rb.AddForce(fallDirection * fallForce, ForceMode.Impulse);  // Kuvvet uygula
            }

            // Ağaç yere düşene kadar bekle
            Rigidbody rbFall = GetComponent<Rigidbody>();
            while (rbFall.velocity.magnitude > 0.1f)
            {
                yield return null;
            }

            isFalling = false;

            StartCoroutine(odunsacma2());
           
        }
    }
    public IEnumerator odunsacma2()
    {
        yield return new WaitForSeconds(2);
        odunsacma.gameObject.SetActive(true);
        odunsacma.transform.parent = null;
        Destroy(gameObject);
    }
}




using System.Collections;
using UnityEngine;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class CowMovement3D : MonoBehaviour
{
    private float timer;
    public float timerDuration = 10f;
    public GameObject prefab;  // Ölüm sonrası oluşturulacak prefab
    public GameObject prefab2; // Süt oluşturulacak prefab
    private Vector3 targetPosition; // Hedef pozisyon
    public Animator animator;
    public float moveSpeed = 2f;   // İneğin hareket hızı
    public float waitTime = 2f;    // İneğin bekleme süresi

    private bool isWalking = false; // İneğin şu anda hareket edip etmediğini kontrol eden değişken

    private Collider myCollider; // İneğin collider'ı

    void Start()
    {
        myCollider = GetComponent<Collider>();
        SetRandomTarget(); // İlk rastgele hedef konumu belirle
        timer = timerDuration; // Zamanlayıcıyı başlat
    }

    void Update()
    {
        MoveToTarget(); // Hedefe doğru hareket et

        // Timer'ı güncelle
        if (timer > 0)
        {
            timer -= Time.deltaTime; // Zamanlayıcıyı güncelle
        }
        else
        {
            myCollider.enabled = true; // Zaman dolduğunda collider'ı etkinleştir
        }
    }

    void MoveToTarget()
    {
        if (isWalking)
        {
            // İneği hedefe doğru hareket ettir
            Vector3 currentPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Hareket yönünü hesapla
            Vector3 direction = (targetPosition - currentPosition).normalized;

            if (direction.magnitude > 0.1f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * moveSpeed);
            }

            AnimateMovement(direction); // Hareket yönüne göre animasyonu güncelle

            // Hedefe ulaşıldıysa
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isWalking = false;
                AnimateMovement(Vector3.zero); // Hedefe ulaştığında animasyonu durdur
                StartCoroutine(WaitAndMove()); // Bir süre bekleyip yeni bir hedefe hareket et
            }
        }
        else
        {
            AnimateMovement(Vector3.zero);
        }
    }

    void SetRandomTarget()
    {
        // Rastgele bir hedef konumu belirle
        float randomX = Random.Range(-5f, 5f);
        float randomZ = Random.Range(-5f, 5f);
        targetPosition = new Vector3(randomX, 0f, randomZ); // Yükseklik sabi
        isWalking = true;
    }

    IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(waitTime);
        SetRandomTarget(); // Yeni bir rastgele hedef belirle
    }

    void AnimateMovement(Vector3 direction)
    {
        if (animator != null)
        {
            if (direction.magnitude > 0.1f)
            {
                animator.SetBool("isMoving", true);
                animator.SetFloat("horizontal", direction.x);
                animator.SetFloat("vertical", direction.z);
            }
            else
            {
                animator.SetBool("isMoving", false);
            }
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("axe"))
        {
            Death();
        }

        if (collision.CompareTag("sagmak"))
        {
            Sagmak();
            timer = timerDuration; // Zamanlayıcıyı sıfırla
            myCollider.enabled = false; // Collider'ı kapat
        }
    }

    void Death()
    {
        Debug.Log("Cow died");

        // Mevcut ineği yok et
        Destroy(gameObject);

        // Belirli bir noktada yeni prefab oluştur
        Vector3 spawnPoint = transform.position;
        Instantiate(prefab, spawnPoint, Quaternion.identity);
    }

    void Sagmak()
    {
        // Süt oluştur
        Vector3 spawnPoint = transform.position;
        Instantiate(prefab2, spawnPoint, Quaternion.identity);
    }
}

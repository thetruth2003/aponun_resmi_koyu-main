using UnityEngine;
using System.Collections;

/// <summary>
/// TreeFall sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class TreeFall : MonoBehaviour
{
    private Vector3 originalPosition;
    public float shakeAmount = 0.015f;
    public bool isFalling = false;
    public float fallForce = 1.5f;
    private int hitCount = 0;
    private bool hasFallen = false;
    public GameObject odunsacma;

    private void Start()
    {

    }

    public IEnumerator ShakeAndFall()
    {
        hitCount++;

        originalPosition = transform.position;
        float shakeDuration = 0.2f;
        float shakeTimer = 0.0f;

        while (shakeTimer < shakeDuration)
        {
            shakeTimer += Time.deltaTime;
            transform.position = originalPosition + Random.insideUnitSphere * shakeAmount;
            yield return null;
        }

        transform.position = originalPosition;

        if (hitCount >= 5 && !isFalling)
        {
            isFalling = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                Vector3 fallDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                rb.AddForce(fallDirection * fallForce, ForceMode.Impulse);
            }

            Rigidbody rbFall = GetComponent<Rigidbody>();
            while (rbFall.linearVelocity.magnitude > 0.1f)
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


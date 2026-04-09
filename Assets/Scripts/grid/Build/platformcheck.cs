using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// platformcheck sinifi, ilgili davranis veya veriyi yonetmek icin kullanilir.
/// </summary>
public class platformcheck : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Platform")
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.transform.tag == "Platform")
        {
            Destroy(gameObject);
        }
    }
}

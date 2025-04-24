using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class socketcheck : MonoBehaviour
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class building : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 50, 0); // X, Y, Z eksenlerinde dönüş hızı (derece/saniye)

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime); // Sürekli dönüş
    }
}


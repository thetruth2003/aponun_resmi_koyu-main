using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Harvester_blade sinifi, arac sistemindeki ilgili davranisi yonetir.
/// </summary>
public class Harvester_blade : MonoBehaviour
{
    private CarController _carController;
    void Start()
    {
        _carController = GetComponentInParent<CarController>();
    }

    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Collectable>()&& _carController.isRotating)
        {
            other.gameObject.GetComponent<Collectable>().Collect();
        }
    }
}

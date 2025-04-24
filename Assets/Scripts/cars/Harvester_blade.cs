using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harvester_blade : MonoBehaviour
{
    private CarController _carController;
    // Start is called before the first frame update
    void Start()
    {
        _carController = GetComponentInParent<CarController>();
    }

    // Update is called once per frame
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

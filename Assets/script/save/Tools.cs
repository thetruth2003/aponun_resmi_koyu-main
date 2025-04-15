using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools : Item
{
    public float duration;
    public string itemName;
    public int price;

    [HideInInspector]
    public Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void EnablePhysics()
    {
        rb.isKinematic = false; // Fiziksel kuvvetleri etkinleştir
    }
}

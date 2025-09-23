using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools : Item
{
    public float duration;
    public string itemName;
    public int price;
    public int sellPrice => price / 2;
    
    public int amount;

}

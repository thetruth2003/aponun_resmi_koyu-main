using TMPro;
using UnityEngine;

/// <summary>
/// Eski para sisteminde mevcut bakiyeyi tutar ve UI yazisini gunceller.
/// </summary>
public class Money : MonoBehaviour
{
    public static Money Instance;
    public int currentMoney = 0;
    public TextMeshProUGUI moneyText;

    private void Start()
    {
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log($"+{amount} eklendi. Yeni bakiye: {currentMoney}");
        UpdateMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log($"-{amount} harcandi. Kalan bakiye: {currentMoney}");
            UpdateMoneyUI();
            return true;
        }

        Debug.LogWarning("Yetersiz bakiye!");
        return false;
    }

    public void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString() + " TL";
        }
    }
}

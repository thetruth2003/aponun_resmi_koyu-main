using UnityEngine;
using TMPro;

public class Money : MonoBehaviour
{
    public static Money Instance;
    public int currentMoney = 0;
    public TextMeshProUGUI moneyText; // UI'daki para yazısı (isteğe bağlı)

    private void Start()
    {
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        Debug.Log($"💰 +{amount}₺ eklendi. Yeni bakiye: {currentMoney}₺");
        UpdateMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            Debug.Log($"💸 -{amount}₺ harcandı. Kalan bakiye: {currentMoney}₺");
            UpdateMoneyUI();
            return true;
        }
        else
        {
            Debug.LogWarning("💢 Yetersiz bakiye!");
            return false;
        }
    }

    public void UpdateMoneyUI()
    {
        if (moneyText != null)
            moneyText.text = currentMoney.ToString() + "₺";
    }
}

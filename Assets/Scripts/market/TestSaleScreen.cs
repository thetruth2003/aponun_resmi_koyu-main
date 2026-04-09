using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test sahnesindeki satis panelini yonetir ve secilen urunun parasini Muhasebeci uzerinden gunceller.
/// </summary>
public class TestSaleScreen : MonoBehaviour
{
    [SerializeField] private Muhasebeci muhasebeci;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button sellButton;
    [SerializeField] private TestSaleDropZone dropZone;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureMoney();
        BindUi();
        Refresh();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureMoney();
        BindUi();
        Refresh();
    }

    private void EnsureMoney()
    {
        if (muhasebeci != null)
        {
            return;
        }

        if (GameManager.instance != null)
        {
            muhasebeci = GameManager.instance.GetComponent<Muhasebeci>();
        }

        if (muhasebeci == null)
        {
            muhasebeci = FindFirstObjectByType<Muhasebeci>();
        }
    }

    private void BindUi()
    {
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(ConfirmSale);
        }
    }

    private void ConfirmSale()
    {
        EnsureMoney();

        if (dropZone == null)
        {
            return;
        }

        int earned = dropZone.ConfirmSale(muhasebeci);
        if (earned <= 0 && infoText != null)
        {
            infoText.text = "Satilacak bir slot birakmadin.";
        }

        Refresh();
    }

    private void Refresh()
    {
        if (moneyText != null && muhasebeci != null)
        {
            moneyText.text = "Para: " + muhasebeci.GetMoney() + " TL";
        }
        else if (moneyText != null)
        {
            moneyText.text = "Para sistemi yok";
        }
    }
}

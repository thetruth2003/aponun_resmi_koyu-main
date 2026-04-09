using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Urun adina gore kilogram basi fiyat tutan basit urun katalog assetidir.
/// </summary>
[CreateAssetMenu(menuName = "Economy/Product Catalog")]
public class ProductCatalog : ScriptableObject
{
    [Serializable]
    /// <summary>
    /// Katalogdaki tek bir urunun adini ve kilogram basi fiyatini saklar.
    /// </summary>
    public struct Product
    {
        public string name;
        public float pricePerKg;
    }

    public List<Product> products = new();

    public bool TryGetPrice(string productName, out float pricePerKg)
    {
        for (int i = 0; i < products.Count; i++)
        {
            if (string.Equals(products[i].name, productName, StringComparison.OrdinalIgnoreCase))
            {
                pricePerKg = products[i].pricePerKg;
                return true;
            }
        }

        pricePerKg = 0f;
        return false;
    }
}

/// <summary>
/// Kesinti ya da ek kalemin nasil hesaplanacagini belirler.
/// </summary>
public enum ChargeMode
{
    Percent,
    PerKg,
    Flat
}

/// <summary>
/// Kesintinin brut tutara mi yoksa ara toplama mi uygulanacagini belirler.
/// </summary>
public enum ChargeBase
{
    Gross,
    Subtotal
}

/// <summary>
/// Fiste gosterilecek tek bir kesinti ya da ek ucret kalemini tanimlar.
/// </summary>
[Serializable]
public class Charge
{
    [Tooltip("Istersen bu kesintiyi gecici olarak kapat.")]
    public bool enabled = true;

    [Tooltip("Fiste gozukcek isim.")]
    public string label = "KDV";

    [Tooltip("Istege bagli kisa not.")]
    [TextArea(1, 2)]
    public string note = "";

    [Tooltip("Percent icin 0.18, PerKg icin kg basi ucret, Flat icin sabit tutar gir.")]
    public ChargeMode mode = ChargeMode.Percent;

    [Tooltip("Yuzde, kilogram basi ya da sabit tutar degeri.")]
    public float value = 0.18f;

    [Tooltip("Brut ya da ara toplam bazini belirler.")]
    public ChargeBase applyOn = ChargeBase.Subtotal;

    [Tooltip("Isaretliyse tutari duser, kapaliysa ekler.")]
    public bool subtract = true;

    [Tooltip("Tutar sifir olsa bile fiste gostermek icin acik birak.")]
    public bool showEvenIfZero = false;
}

/// <summary>
/// Fiste gorunen tek satirlik tutar bilgisini saklar.
/// </summary>
[Serializable]
public class ReceiptLine
{
    public string label;
    public float amount;
}

/// <summary>
/// Satis ya da alis isleminden sonra olusan ozet fis verisidir.
/// </summary>
[Serializable]
public class Receipt
{
    public string title;
    public string productName;
    public float quantityKg;
    public float unitPrice;
    public float gross;
    public List<ReceiptLine> lines = new();
    public float totalDeductions;
    public float net;

    public void Add(string label, float amount)
    {
        lines.Add(new ReceiptLine { label = label, amount = amount });
    }
}

/// <summary>
/// Oyundaki para, satis, alis ve fis hesaplarini merkezi olarak yoneten ana muhasebe sinifidir.
/// </summary>
public class Muhasebeci : MonoBehaviour
{
    [Header("Bagimliliklar")]
    [Tooltip("Urun adlari ve kilogram basi fiyatlarin bulundugu katalog.")]
    public ProductCatalog productCatalog;

    [Tooltip("Para yazisini gosteren UI nesnesi.")]
    public TextMeshProUGUI moneyText;

    [Header("Kasadaki Para")]
    public int playerMoney = 0;

    [Header("Satis Kalemleri (sira onemlidir)")]
    [TextArea(2, 4)]
    public string saleNote =
        "Onerilen sira: Toptanci (Brut), Hal Ucreti (Ara Toplam), Nakliye (Kg basi), Vergi/KDV (Ara Toplam), Damga (Ara Toplam), Bakim (Brut), Hane (Brut).";

    public List<Charge> saleCharges = new();

    [Header("Alis Kalemleri (sira onemlidir)")]
    [TextArea(2, 4)]
    public string purchaseNote =
        "Tohum, gubre, ilac gibi alislarda uygulanan ek kalemler. Percent icin 0.10 = %10.";

    public List<Charge> purchaseCharges = new();

    /// <summary>
    /// ReceiptEvent sinifi, pazar ve ekonomi akislarinda kullanilan ilgili davranisi yonetir.
    /// </summary>
    [Serializable]
    public class ReceiptEvent : UnityEngine.Events.UnityEvent<Receipt> { }

    [Header("Olaylar")]
    public ReceiptEvent OnReceiptCreated;

    private void Awake()
    {
        if (moneyText != null)
        {
            moneyText.text = playerMoney.ToString();
        }
    }

    public int GetMoney()
    {
        return playerMoney;
    }

    public void SetMoney(int value)
    {
        playerMoney = Mathf.Max(0, value);

        if (moneyText != null)
        {
            moneyText.text = playerMoney.ToString();
        }
    }

    public Receipt Sell(string productName, float quantityKg)
    {
        if (productCatalog == null)
        {
            Debug.LogError("[Muhasebeci] ProductCatalog atanmadi.");
            return null;
        }

        if (!productCatalog.TryGetPrice(productName, out float unitPrice))
        {
            Debug.LogError($"[Muhasebeci] Katalogda urun yok: {productName}");
            return null;
        }

        Receipt receipt = new Receipt
        {
            title = "SATIS FISI",
            productName = productName,
            quantityKg = quantityKg,
            unitPrice = unitPrice,
            gross = quantityKg * unitPrice
        };

        float subtotal = receipt.gross;

        foreach (Charge charge in saleCharges)
        {
            if (charge == null || !charge.enabled)
            {
                continue;
            }

            float baseValue = charge.applyOn == ChargeBase.Gross ? receipt.gross : subtotal;
            float amount = 0f;

            switch (charge.mode)
            {
                case ChargeMode.Percent:
                    amount = baseValue * charge.value;
                    break;
                case ChargeMode.PerKg:
                    amount = quantityKg * charge.value;
                    break;
                case ChargeMode.Flat:
                    amount = charge.value;
                    break;
            }

            if (charge.subtract)
            {
                amount = -Mathf.Abs(amount);
            }

            if (amount != 0f || charge.showEvenIfZero)
            {
                receipt.Add(charge.label, amount);
            }

            subtotal += amount;
        }

        receipt.totalDeductions = Mathf.Max(0f, receipt.gross - Mathf.Max(0f, subtotal));
        receipt.net = Mathf.Max(0f, subtotal);

        ApplyMoneyDelta(Mathf.RoundToInt(receipt.net));
        OnReceiptCreated?.Invoke(receipt);
        return receipt;
    }

    public Receipt Purchase(string label, float basePrice)
    {
        Receipt receipt = new Receipt
        {
            title = "ALIS FISI",
            productName = label,
            quantityKg = 1f,
            unitPrice = basePrice,
            gross = basePrice
        };

        float subtotal = receipt.gross;

        foreach (Charge charge in purchaseCharges)
        {
            if (charge == null || !charge.enabled)
            {
                continue;
            }

            float baseValue = charge.applyOn == ChargeBase.Gross ? receipt.gross : subtotal;
            float amount = 0f;

            switch (charge.mode)
            {
                case ChargeMode.Percent:
                    amount = baseValue * charge.value;
                    break;
                case ChargeMode.PerKg:
                    amount = 0f;
                    break;
                case ChargeMode.Flat:
                    amount = charge.value;
                    break;
            }

            if (charge.subtract)
            {
                amount = -Mathf.Abs(amount);
            }

            if (amount != 0f || charge.showEvenIfZero)
            {
                receipt.Add(charge.label, amount);
            }

            subtotal += amount;
        }

        receipt.totalDeductions = Mathf.Max(0f, receipt.gross - Mathf.Max(0f, subtotal));
        receipt.net = Mathf.Max(0f, subtotal);

        ApplyMoneyDelta(-Mathf.RoundToInt(receipt.net));
        OnReceiptCreated?.Invoke(receipt);
        return receipt;
    }

    private void ApplyMoneyDelta(int delta)
    {
        playerMoney += delta;

        if (playerMoney < 0)
        {
            playerMoney = 0;
        }

        if (moneyText != null)
        {
            moneyText.text = playerMoney.ToString();
        }
    }

    public void AddMoney(int amount)
    {
        ApplyMoneyDelta(amount);
    }

    [ContextMenu("Satis Preseti/Yumusak")]
    public void LoadSoftSaleCharges()
    {
        saleCharges = new List<Charge>
        {
            new Charge { label = "Toptanci Komisyonu", note = "Araci payi", mode = ChargeMode.Percent, value = 0.10f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Hal Giris Kesintisi", note = "Pazar giris masrafi", mode = ChargeMode.Percent, value = 0.04f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Nakliye", note = "Kamyon + tasima", mode = ChargeMode.PerKg, value = 0.25f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Vergi", note = "Oyunsal genel vergi", mode = ChargeMode.Percent, value = 0.08f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Bakim Payi", note = "Mazot, bakim, yipranma", mode = ChargeMode.Percent, value = 0.04f, applyOn = ChargeBase.Gross, subtract = true }
        };
    }

    [ContextMenu("Satis Preseti/Acimasiz")]
    public void LoadHarshSaleCharges()
    {
        saleCharges = new List<Charge>
        {
            new Charge { label = "Toptanci Komisyonu", note = "Araci senden iyi kazaniyor", mode = ChargeMode.Percent, value = 0.15f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Hal Giris Kesintisi", note = "Iceri girmek bile masraf", mode = ChargeMode.Percent, value = 0.06f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Nakliye", note = "Yol parasini yine sen oduyorsun", mode = ChargeMode.PerKg, value = 0.40f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Vergi", note = "Genel vergi yuku", mode = ChargeMode.Percent, value = 0.10f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Damga", note = "Evrak sever sistem", mode = ChargeMode.Percent, value = 0.02f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Bakim/Amortisman", note = "Ekipman agliyor", mode = ChargeMode.Percent, value = 0.05f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Hane Payi", note = "Ev de bu isten yiyor", mode = ChargeMode.Percent, value = 0.08f, applyOn = ChargeBase.Gross, subtract = true }
        };
    }

    [ContextMenu("Satis Preseti/Recep Modu")]
    public void LoadRecepSaleCharges()
    {
        saleCharges = new List<Charge>
        {
            new Charge { label = "Toptanci Komisyonu", note = "Daha bastan tokat", mode = ChargeMode.Percent, value = 0.18f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Hal Giris Kesintisi", note = "Kapidan girerken yazildi", mode = ChargeMode.Percent, value = 0.07f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Nakliye", note = "Mazot ayri dert", mode = ChargeMode.PerKg, value = 0.55f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Vergi", note = "Normal vergi", mode = ChargeMode.Percent, value = 0.12f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Damga", note = "Kagit kagit ustune", mode = ChargeMode.Percent, value = 0.03f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Bakim/Amortisman", note = "Bicer, ekipman, mazot", mode = ChargeMode.Percent, value = 0.06f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Hane Payi", note = "Ev de bu satistan gecinecek", mode = ChargeMode.Percent, value = 0.10f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Recep Payi", note = "Sebebi sorulmaz", mode = ChargeMode.Percent, value = 0.15f, applyOn = ChargeBase.Subtotal, subtract = true }
        };
    }

    [ContextMenu("Satis Kesintilerini Temizle")]
    public void ClearSaleCharges()
    {
        saleCharges = new List<Charge>();
    }

    [ContextMenu("Alis Preseti/Standart")]
    public void LoadStandardPurchaseCharges()
    {
        purchaseCharges = new List<Charge>
        {
            new Charge { label = "Bayi Komisyonu", note = "Tedarikci payi", mode = ChargeMode.Percent, value = 0.05f, applyOn = ChargeBase.Gross, subtract = true },
            new Charge { label = "Vergi", note = "Oyunsal alis vergisi", mode = ChargeMode.Percent, value = 0.08f, applyOn = ChargeBase.Subtotal, subtract = true },
            new Charge { label = "Tasima", note = "Urun getirme masrafi", mode = ChargeMode.Flat, value = 15f, applyOn = ChargeBase.Subtotal, subtract = true }
        };
    }

    [ContextMenu("Alis Kesintilerini Temizle")]
    public void ClearPurchaseCharges()
    {
        purchaseCharges = new List<Charge>();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Ürün kataloğu: ürün adı -> kg başı satış fiyatı.
/// Inspector'dan listeyi doldur: "Add Product" bas, ad & fiyat gir.
/// </summary>
[CreateAssetMenu(menuName = "Economy/Product Catalog")]
public class ProductCatalog : ScriptableObject
{
    [Serializable]
    public struct Product
    {
        public string name;
        public float pricePerKg; // oyun parası/kg
    }

    public List<Product> products = new();

    public bool TryGetPrice(string productName, out float pricePerKg)
    {
        for (int i = 0; i < products.Count; i++)
            if (string.Equals(products[i].name, productName, StringComparison.OrdinalIgnoreCase))
            {
                pricePerKg = products[i].pricePerKg;
                return true;
            }
        pricePerKg = 0f;
        return false;
    }
}

/// <summary>
/// Kesinti/ek kalem hesaplama modu.
/// </summary>
public enum ChargeMode { Percent, PerKg, Flat }

/// <summary>
/// Uygulanacak baz (vergilerin vergisi için Subtotal çok kritik)
/// </summary>
public enum ChargeBase { Gross, Subtotal }

/// <summary>
/// Fişte görünecek kalem (ör: Toptancı Komisyonu, KDV, Nakliye)
/// </summary>
[Serializable]
public class Charge
{
    [Tooltip("Fişte gözükecek isim (örn: Toptancı Komisyonu)")]
    public string label = "KDV";

    [Tooltip("% için 0.18 yaz (%18), PerKg için kg başı ücret, Flat için sabit tutar")]
    public ChargeMode mode = ChargeMode.Percent;

    [Tooltip("Percent: 0.18 (%18) | PerKg: 0.40 (kg başı) | Flat: 50 (sabit)")]
    public float value = 0.18f;

    [Tooltip("Gross: brüt | Subtotal: o ana kadarki ara toplam (vergilerin vergisi)")]
    public ChargeBase applyOn = ChargeBase.Subtotal;

    [Tooltip("true: düş (kesinti) | false: ekle (nadiren)")]
    public bool subtract = true;

    [Tooltip("0 bile olsa fişte görünmesini istiyorsan işaretle")]
    public bool showEvenIfZero = false;
}

/// <summary>
/// Fiş satırı (kalem + miktar)
/// </summary>
[Serializable]
public class ReceiptLine
{
    public string label;
    public float amount; // negatifse kesinti
}

/// <summary>
/// Satış/Alış fişi
/// </summary>
[Serializable]
public class Receipt
{
    public string title;         // "SATIŞ FİŞİ" / "ALIŞ FİŞİ"
    public string productName;
    public float quantityKg;
    public float unitPrice;      // kg başı
    public float gross;          // brüt: quantityKg * unitPrice
    public List<ReceiptLine> lines = new();
    public float totalDeductions; // kesintilerin toplam mutlak değeri
    public float net;             // net: cebine giren/çıkan

    public void Add(string label, float amount) => lines.Add(new ReceiptLine { label = label, amount = amount });
}

/// <summary>
/// Tüm para işlerinin merkezi: Muhasebeci
/// - Satış/alış hesabı (vergilerin vergisi, komisyon vb.)
/// - Fiş üretimi & event yayını (UI bağla)
/// - Kasaya para işleme
/// </summary>
public class Muhasebeci : MonoBehaviour
{
    [Header("Bağımlılıklar")]
    [Tooltip("Ürün adları -> kg başı satış fiyatları")]
    public ProductCatalog productCatalog;

    [Tooltip("Oyuncu para UI (opsiyonel: null bırakabilirsin)")]
    public TextMeshProUGUI moneyText;

    [Header("Kasadaki para")]
    public int playerMoney = 0;

    [Header("Satış Kalemleri (sıra önemlidir)")]
    [TextArea(1, 2)] public string saleNote = "Önerilen sıra: Toptancı (Gross), Hal Ücreti (Subtotal), Nakliye (PerKg), KDV (Subtotal), Damga (Subtotal), Bakım (Gross), Hane (Gross)";
    public List<Charge> saleCharges = new();

    [Header("Alış Kalemleri (sıra önemlidir)")]
    public List<Charge> purchaseCharges = new();

    // --- Event: Fiş üretildiğinde UI/Log yakalasın ---
    [Serializable] public class ReceiptEvent : UnityEngine.Events.UnityEvent<Receipt> { }
    [Header("Olaylar")]
    public ReceiptEvent OnReceiptCreated;

    void Awake()
    {
        if (moneyText != null) moneyText.text = playerMoney.ToString();
    }

    // =========================
    //  SATIŞ
    // =========================
    public Receipt Sell(string productName, float quantityKg)
    {
        if (productCatalog == null)
        {
            Debug.LogError("[Muhasebeci] ProductCatalog atanmadı.");
            return null;
        }
        if (!productCatalog.TryGetPrice(productName, out float unitPrice))
        {
            Debug.LogError($"[Muhasebeci] Katalogda ürün yok: {productName}");
            return null;
        }

        var r = new Receipt
        {
            title = "SATIŞ FİŞİ",
            productName = productName,
            quantityKg = quantityKg,
            unitPrice = unitPrice,
            gross = quantityKg * unitPrice
        };

        float subtotal = r.gross;

        // SIRA ÖNEMLİ: listedeki sırayla uygula (vergilerin vergisi için Subtotal kritik)
        foreach (var ch in saleCharges)
        {
            float baseVal = (ch.applyOn == ChargeBase.Gross) ? r.gross : subtotal;
            float amt = 0f;

            switch (ch.mode)
            {
                case ChargeMode.Percent: amt = baseVal * ch.value; break;
                case ChargeMode.PerKg:   amt = quantityKg * ch.value; break;
                case ChargeMode.Flat:    amt = ch.value; break;
            }

            if (ch.subtract) amt = -Mathf.Abs(amt);
            if (amt != 0f || ch.showEvenIfZero) r.Add(ch.label, amt);

            subtotal += amt; // Subtotal bazlı kalemler birbirini etkiler
        }

        r.totalDeductions = Mathf.Max(0f, r.gross - Mathf.Max(0f, subtotal));
        r.net = Mathf.Max(0f, subtotal);

        // Kasaya işle
        ApplyMoneyDelta(Mathf.RoundToInt(r.net));

        // Event
        OnReceiptCreated?.Invoke(r);
        return r;
    }

    // =========================
    //  ALIŞ (tohum, gübre, ekipman vs.)
    // =========================
    public Receipt Purchase(string label, float basePrice)
    {
        var r = new Receipt
        {
            title = "ALIŞ FİŞİ",
            productName = label,
            quantityKg = 1f,
            unitPrice = basePrice,
            gross = basePrice
        };

        float subtotal = r.gross;

        foreach (var ch in purchaseCharges)
        {
            float baseVal = (ch.applyOn == ChargeBase.Gross) ? r.gross : subtotal;
            float amt = 0f;

            switch (ch.mode)
            {
                case ChargeMode.Percent: amt = baseVal * ch.value; break;
                case ChargeMode.PerKg:   amt = 0f; break; // alışta kg yoksa kullanma
                case ChargeMode.Flat:    amt = ch.value; break;
            }

            if (ch.subtract) amt = -Mathf.Abs(amt);
            if (amt != 0f || ch.showEvenIfZero) r.Add(ch.label, amt);

            subtotal += amt;
        }

        r.totalDeductions = Mathf.Max(0f, r.gross - Mathf.Max(0f, subtotal));
        r.net = Mathf.Max(0f, subtotal);

        // Kasadan düş
        ApplyMoneyDelta(-Mathf.RoundToInt(r.net));

        OnReceiptCreated?.Invoke(r);
        return r;
    }

    // =========================
    //  Ortak: parayı uygula ve UI güncelle
    // =========================
    private void ApplyMoneyDelta(int delta)
    {
        playerMoney += delta;
        if (playerMoney < 0) playerMoney = 0;
        if (moneyText != null) moneyText.text = playerMoney.ToString();
    }
    public void AddMoney(int amount)
    {
        ApplyMoneyDelta(amount);
    }
    // =========================
    //  Yardımcı: örnek charge presetleri (Inspector'da buton yerine çağır)
    // =========================
    [ContextMenu("Örnek Satış Kalemlerini Yükle")]
    public void LoadExampleSaleCharges()
    {
        saleCharges = new List<Charge>
        {
            new Charge{ label="Toptancı Komisyonu", mode=ChargeMode.Percent, value=0.15f, applyOn=ChargeBase.Gross,    subtract=true },
            new Charge{ label="Hal Giriş Ücreti",    mode=ChargeMode.Percent, value=0.05f, applyOn=ChargeBase.Subtotal, subtract=true },
            new Charge{ label="Nakliye",             mode=ChargeMode.PerKg,   value=0.40f, applyOn=ChargeBase.Subtotal, subtract=true },
            new Charge{ label="KDV",                 mode=ChargeMode.Percent, value=0.10f, applyOn=ChargeBase.Subtotal, subtract=true },
            new Charge{ label="Damga Vergisi",       mode=ChargeMode.Percent, value=0.02f, applyOn=ChargeBase.Subtotal, subtract=true },
            new Charge{ label="Bakım/Amortisman",    mode=ChargeMode.Percent, value=0.05f, applyOn=ChargeBase.Gross,    subtract=true },
            new Charge{ label="Hane Gideri Payı",    mode=ChargeMode.Percent, value=0.10f, applyOn=ChargeBase.Gross,    subtract=true },
        };
    }

    [ContextMenu("Örnek Alış Kalemlerini Yükle")]
    public void LoadExamplePurchaseCharges()
    {
        purchaseCharges = new List<Charge>
        {
            new Charge{ label="Bayi Komisyonu", mode=ChargeMode.Percent, value=0.05f, applyOn=ChargeBase.Gross,    subtract=true },
            new Charge{ label="ÖTV",            mode=ChargeMode.Percent, value=0.01f, applyOn=ChargeBase.Subtotal, subtract=true },
            new Charge{ label="KDV",            mode=ChargeMode.Percent, value=0.10f, applyOn=ChargeBase.Subtotal, subtract=true },
        };
    }
}

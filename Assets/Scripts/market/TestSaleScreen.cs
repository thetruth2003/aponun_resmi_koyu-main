using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TestSaleScreen : MonoBehaviour
{
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button sellButton;
    [SerializeField] private TestSaleDropZone dropZone;
    [SerializeField] private TMP_FontAsset cachedFont;

    private Muhasebeci muhasebeci;

    private void OnEnable()
    {
        EnsureMoney();
        BuildUiIfNeeded();
        Refresh();
    }

    private void OnValidate()
    {
        EnsureMoney();
        BuildUiIfNeeded();
        Refresh();
    }

    private void EnsureMoney()
    {
        muhasebeci = FindFirstObjectByType<Muhasebeci>();
        if (muhasebeci == null)
        {
            GameObject moneyObject = GameObject.Find("Muhasebeci_Test");
            if (moneyObject == null)
            {
                moneyObject = new GameObject("Muhasebeci_Test");
            }

            muhasebeci = moneyObject.GetComponent<Muhasebeci>();
            if (muhasebeci == null)
            {
                muhasebeci = moneyObject.AddComponent<Muhasebeci>();
                muhasebeci.playerMoney = 5000;
            }
        }
    }

    private void BuildUiIfNeeded()
    {
        RectTransform host = transform as RectTransform;
        if (host == null)
        {
            return;
        }

        host.anchorMin = new Vector2(0f, 1f);
        host.anchorMax = new Vector2(0f, 1f);
        host.pivot = new Vector2(0f, 1f);
        host.anchoredPosition = new Vector2(24f, -24f);
        host.sizeDelta = new Vector2(360f, 420f);

        panelRoot = host;
        Image background = GetOrAddComponent<Image>(panelRoot.gameObject);
        background.color = new Color(0.1f, 0.08f, 0.06f, 0.94f);

        VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(panelRoot.gameObject);
        layout.spacing = 8f;
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        LayoutElement layoutElement = GetOrAddComponent<LayoutElement>(panelRoot.gameObject);
        layoutElement.preferredWidth = 360f;
        layoutElement.preferredHeight = 420f;

        TextMeshProUGUI title = EnsureText("Title", panelRoot, 26f, FontStyles.Bold);
        title.text = "Satis Proto";

        moneyText = EnsureText("MoneyText", panelRoot, 21f, FontStyles.Bold);
        moneyText.color = new Color(0.95f, 0.88f, 0.58f);

        TextMeshProUGUI hint = EnsureText("HintText", panelRoot, 16f, FontStyles.Italic);
        hint.enableWordWrapping = true;
        hint.text = "Envanter slotunu buraya surukle. Shift basiliysa 1 adet, degilse tum miktar satilir.";

        RectTransform dropRoot = EnsureRect("DropZone", panelRoot);
        LayoutElement dropLayout = GetOrAddComponent<LayoutElement>(dropRoot.gameObject);
        dropLayout.preferredHeight = 210f;

        Image dropBackground = GetOrAddComponent<Image>(dropRoot.gameObject);
        dropBackground.color = new Color(0.18f, 0.2f, 0.24f, 0.85f);

        VerticalLayoutGroup dropGroup = GetOrAddComponent<VerticalLayoutGroup>(dropRoot.gameObject);
        dropGroup.spacing = 6f;
        dropGroup.padding = new RectOffset(12, 12, 12, 12);
        dropGroup.childAlignment = TextAnchor.UpperLeft;
        dropGroup.childControlHeight = false;
        dropGroup.childControlWidth = true;
        dropGroup.childForceExpandHeight = false;
        dropGroup.childForceExpandWidth = true;

        Image preview = GetOrAddComponent<Image>(EnsureRect("PreviewIcon", dropRoot).gameObject);
        preview.rectTransform.sizeDelta = new Vector2(72f, 72f);
        preview.color = new Color(1f, 1f, 1f, 0f);

        TextMeshProUGUI itemName = EnsureText("ItemName", dropRoot, 22f, FontStyles.Bold);
        TextMeshProUGUI quantity = EnsureText("Quantity", dropRoot, 18f, FontStyles.Normal);
        TextMeshProUGUI unitPrice = EnsureText("UnitPrice", dropRoot, 18f, FontStyles.Normal);
        TextMeshProUGUI totalPrice = EnsureText("TotalPrice", dropRoot, 20f, FontStyles.Bold);
        totalPrice.color = new Color(0.73f, 0.95f, 0.7f);

        infoText = EnsureText("InfoText", panelRoot, 17f, FontStyles.Normal);
        infoText.enableWordWrapping = true;

        sellButton = EnsureButton("SellButton", panelRoot, "Sat");

        dropZone = GetOrAddComponent<TestSaleDropZone>(dropRoot.gameObject);
        dropZone.Setup(preview, itemName, quantity, unitPrice, totalPrice, infoText, dropBackground);

        sellButton.onClick.RemoveAllListeners();
        sellButton.onClick.AddListener(ConfirmSale);
    }

    private void ConfirmSale()
    {
        EnsureMoney();
        if (dropZone == null)
        {
            return;
        }

        int earned = dropZone.ConfirmSale(muhasebeci);
        if (earned <= 0)
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
    }

    private TextMeshProUGUI EnsureText(string name, Transform parent, float fontSize, FontStyles style)
    {
        RectTransform rect = EnsureRect(name, parent);
        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
        text.font = GetFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.text = name;
        return text;
    }

    private Button EnsureButton(string name, Transform parent, string label)
    {
        RectTransform rect = EnsureRect(name, parent);
        rect.sizeDelta = new Vector2(160f, 42f);

        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = new Color(0.48f, 0.28f, 0.12f, 1f);

        Button button = GetOrAddComponent<Button>(rect.gameObject);

        RectTransform labelRect = EnsureRect("Label", rect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = GetOrAddComponent<TextMeshProUGUI>(labelRect.gameObject);
        buttonText.font = GetFont();
        buttonText.fontSize = 20f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.text = label;
        return button;
    }

    private RectTransform EnsureRect(string name, Transform parent)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            child = go.transform;
            child.SetParent(parent, false);
        }

        return child as RectTransform;
    }

    private TMP_FontAsset GetFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = TMP_Settings.defaultFontAsset;
        if (cachedFont == null)
        {
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        return cachedFont;
    }

    private T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}

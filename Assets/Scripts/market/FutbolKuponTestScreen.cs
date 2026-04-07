using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FutbolKuponTestScreen : MonoBehaviour
{
    private enum KuponPage
    {
        Overview,
        Today,
        Tomorrow,
        Results
    }

    [System.Serializable]
    private class MatchRowUi
    {
        public RectTransform root;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI oddsText;
        public TextMeshProUGUI infoText;
        public Button homeButton;
        public Button drawButton;
        public Button awayButton;
    }

    [SerializeField] private FutbolKuponManager manager;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI stakeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button minusStakeButton;
    [SerializeField] private Button plusStakeButton;
    [SerializeField] private Button advanceDayButton;
    [SerializeField] private Button overviewTabButton;
    [SerializeField] private Button todayTabButton;
    [SerializeField] private Button tomorrowTabButton;
    [SerializeField] private Button resultsTabButton;
    [SerializeField] private RectTransform overviewPageRoot;
    [SerializeField] private RectTransform todayPageRoot;
    [SerializeField] private RectTransform tomorrowPageRoot;
    [SerializeField] private RectTransform resultsPageRoot;
    [SerializeField] private MatchRowUi[] todayRows = new MatchRowUi[2];
    [SerializeField] private MatchRowUi[] tomorrowRows = new MatchRowUi[2];

    private int currentStake = 100;
    private TMP_FontAsset cachedFont;
    private bool listenersBound;
    private KuponPage currentPage = KuponPage.Overview;

    private void OnEnable()
    {
        EnsureManagerAndMoney();
        BuildUiIfNeeded();
        Refresh();

        if (Application.isPlaying && manager != null)
        {
            manager.OnStateChanged -= Refresh;
            manager.OnStateChanged += Refresh;
            BindListeners();
        }
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.OnStateChanged -= Refresh;
        }

        UnbindListeners();
    }

    private void OnValidate()
    {
        currentStake = Mathf.Max(50, currentStake);
        EnsureManagerAndMoney();
        BuildUiIfNeeded();
        Refresh();
    }

    private void EnsureManagerAndMoney()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<FutbolKuponManager>();
        }

        if (manager == null)
        {
            GameObject managerObject = GameObject.Find("FutbolKuponManager");
            if (managerObject == null)
            {
                managerObject = new GameObject("FutbolKuponManager");
            }

            manager = managerObject.GetComponent<FutbolKuponManager>();
            if (manager == null)
            {
                manager = managerObject.AddComponent<FutbolKuponManager>();
            }
        }

        Muhasebeci muhasebeci = FindFirstObjectByType<Muhasebeci>();
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

        manager.ForceRefresh();
    }

    private void BuildUiIfNeeded()
    {
        RectTransform host = transform as RectTransform;
        if (host == null)
        {
            return;
        }

        host.anchorMin = new Vector2(1f, 1f);
        host.anchorMax = new Vector2(1f, 1f);
        host.pivot = new Vector2(1f, 1f);
        host.anchoredPosition = new Vector2(-18f, -18f);
        host.sizeDelta = new Vector2(380f, 560f);

        panelRoot = host;
        EnsurePanelStyle(panelRoot);

        headerText = EnsureText("Header", panelRoot, 22, FontStyles.Bold);
        headerText.text = "Kahve Kuponu";

        moneyText = EnsureText("Money", panelRoot, 18, FontStyles.Bold);
        moneyText.color = new Color(0.94f, 0.88f, 0.56f);

        RectTransform stakeRow = EnsureRow("StakeRow", panelRoot);
        minusStakeButton = EnsureButton("MinusStake", stakeRow, "-50");
        stakeText = EnsureText("StakeValue", stakeRow, 18, FontStyles.Bold);
        plusStakeButton = EnsureButton("PlusStake", stakeRow, "+50");

        RectTransform tabRow = EnsureRow("TabRow", panelRoot);
        overviewTabButton = EnsureButton("OverviewTab", tabRow, "Genel");
        todayTabButton = EnsureButton("TodayTab", tabRow, "Bugun");
        tomorrowTabButton = EnsureButton("TomorrowTab", tabRow, "Yarin");
        resultsTabButton = EnsureButton("ResultsTab", tabRow, "Sonuclar");

        statusText = EnsureText("Status", panelRoot, 14, FontStyles.Normal);
        statusText.enableWordWrapping = true;

        overviewPageRoot = EnsureBox("OverviewPage", panelRoot);
        todayPageRoot = EnsureBox("TodayPage", panelRoot);
        tomorrowPageRoot = EnsureBox("TomorrowPage", panelRoot);
        resultsPageRoot = EnsureBox("ResultsPage", panelRoot);

        EnsureSectionLabel("OverviewHeader", overviewPageRoot, "Kupon Ozeti");
        TextMeshProUGUI overviewInfo = EnsureText("OverviewInfo", overviewPageRoot, 14, FontStyles.Normal);
        overviewInfo.enableWordWrapping = true;
        overviewInfo.text =
            "Her gun 2 mac uretilir.\n" +
            "Oranlar takim gucu ve illegal etki ihtimaline gore hesaplanir.\n" +
            "Once bahsi kur, sonra gunu bitir.";

        TextMeshProUGUI illegalInfo = EnsureText("IllegalInfo", overviewPageRoot, 14, FontStyles.Italic);
        illegalInfo.enableWordWrapping = true;
        illegalInfo.text = "Illegal etki bazen gucsuz takimi sisirir, bazen favoriyi daha da iter.";

        EnsureSectionLabel("TodayHeader", todayPageRoot, "Bugunun Maclari");
        for (int i = 0; i < todayRows.Length; i++)
        {
            todayRows[i] = EnsureMatchRow("TodayRow_" + i, todayPageRoot, true);
        }

        EnsureSectionLabel("TomorrowHeader", tomorrowPageRoot, "Yarinin Maclari");
        for (int i = 0; i < tomorrowRows.Length; i++)
        {
            tomorrowRows[i] = EnsureMatchRow("TomorrowRow_" + i, tomorrowPageRoot, false);
        }

        EnsureSectionLabel("ResultsHeader", resultsPageRoot, "Bugun Bitenler");
        resultText = EnsureText("Results", resultsPageRoot, 13, FontStyles.Normal);
        resultText.enableWordWrapping = true;
        resultText.alignment = TextAlignmentOptions.TopLeft;

        advanceDayButton = EnsureButton("AdvanceDay", panelRoot, "Gunu Bitir / Sonuclari Isle");
        ApplyPageVisibility();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
    }

    private void BindListeners()
    {
        if (listenersBound)
        {
            UnbindListeners();
        }

        if (minusStakeButton != null) minusStakeButton.onClick.AddListener(OnMinusStake);
        if (plusStakeButton != null) plusStakeButton.onClick.AddListener(OnPlusStake);
        if (advanceDayButton != null) advanceDayButton.onClick.AddListener(OnAdvanceDay);
        if (overviewTabButton != null) overviewTabButton.onClick.AddListener(ShowOverviewPage);
        if (todayTabButton != null) todayTabButton.onClick.AddListener(ShowTodayPage);
        if (tomorrowTabButton != null) tomorrowTabButton.onClick.AddListener(ShowTomorrowPage);
        if (resultsTabButton != null) resultsTabButton.onClick.AddListener(ShowResultsPage);

        for (int i = 0; i < todayRows.Length; i++)
        {
            int index = i;
            if (todayRows[i].homeButton != null) todayRows[i].homeButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.HomeWin));
            if (todayRows[i].drawButton != null) todayRows[i].drawButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.Draw));
            if (todayRows[i].awayButton != null) todayRows[i].awayButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.AwayWin));
        }

        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (minusStakeButton != null) minusStakeButton.onClick.RemoveAllListeners();
        if (plusStakeButton != null) plusStakeButton.onClick.RemoveAllListeners();
        if (advanceDayButton != null) advanceDayButton.onClick.RemoveAllListeners();
        if (overviewTabButton != null) overviewTabButton.onClick.RemoveAllListeners();
        if (todayTabButton != null) todayTabButton.onClick.RemoveAllListeners();
        if (tomorrowTabButton != null) tomorrowTabButton.onClick.RemoveAllListeners();
        if (resultsTabButton != null) resultsTabButton.onClick.RemoveAllListeners();

        for (int i = 0; i < todayRows.Length; i++)
        {
            if (todayRows[i] == null)
            {
                continue;
            }

            if (todayRows[i].homeButton != null) todayRows[i].homeButton.onClick.RemoveAllListeners();
            if (todayRows[i].drawButton != null) todayRows[i].drawButton.onClick.RemoveAllListeners();
            if (todayRows[i].awayButton != null) todayRows[i].awayButton.onClick.RemoveAllListeners();
        }

        listenersBound = false;
    }

    private void Refresh()
    {
        if (headerText == null)
        {
            return;
        }

        if (manager == null)
        {
            headerText.text = "Kahve Kuponu";
            if (moneyText != null) moneyText.text = "Manager bulunamadi.";
            return;
        }

        if (!Application.isPlaying)
        {
            headerText.text = "Kahve Kuponu";
            moneyText.text = "Play Mode'da guncel veriler dolar.";
            stakeText.text = currentStake + " TL";
            statusText.text = "Bugun 2 mac, yarin 2 mac. Bahisler guce ve illegal etkiye gore simule edilir.";
            resultText.text = "Play Mode'da sonuclar burada gorunur.";
            return;
        }

        headerText.text = "Kahve Kuponu | Gun " + manager.CurrentDay;
        moneyText.text = "Para: " + manager.GetCurrentMoney() + " TL";
        stakeText.text = currentStake + " TL";
        if (string.IsNullOrWhiteSpace(statusText.text))
        {
            statusText.text = "Bir mac secip bahis yap.";
        }

        var todays = manager.TodayMatches;
        var tomorrows = manager.TomorrowMatches;

        for (int i = 0; i < todayRows.Length; i++)
        {
            bool hasData = i < todays.Count;
            SetRowData(todayRows[i], hasData ? todays[i] : null, hasData, true, i);
        }

        for (int i = 0; i < tomorrowRows.Length; i++)
        {
            bool hasData = i < tomorrows.Count;
            SetRowData(tomorrowRows[i], hasData ? tomorrows[i] : null, hasData, false, i);
        }

        resultText.text = string.IsNullOrWhiteSpace(manager.LatestResultsSummary)
            ? "Henuz gun sonu yok."
            : manager.LatestResultsSummary;

        ApplyPageVisibility();
    }

    private void SetRowData(MatchRowUi row, FutbolKuponManager.MatchCard match, bool visible, bool interactive, int rowIndex)
    {
        if (row == null || row.root == null)
        {
            return;
        }

        row.root.gameObject.SetActive(visible);
        if (!visible || match == null)
        {
            return;
        }

        row.titleText.text = match.homeTeam + "  vs  " + match.awayTeam;
        row.oddsText.text = manager.FormatOdds(match);

        if (interactive)
        {
            row.infoText.text = manager.GetTicketSummaryForTodayMatch(rowIndex);
        }
        else
        {
            row.infoText.text = match.illegalInfluenceUsed
                ? "Illegal suphe: " + match.illegalFavoredTeam
                : "Illegal etki yok";
        }

        if (row.homeButton != null) row.homeButton.gameObject.SetActive(interactive);
        if (row.drawButton != null) row.drawButton.gameObject.SetActive(interactive);
        if (row.awayButton != null) row.awayButton.gameObject.SetActive(interactive);
    }

    private void OnMinusStake()
    {
        currentStake = Mathf.Max(50, currentStake - 50);
        Refresh();
    }

    private void OnPlusStake()
    {
        currentStake = Mathf.Min(5000, currentStake + 50);
        Refresh();
    }

    private void OnAdvanceDay()
    {
        manager.AdvanceDay();
        statusText.text = "Gun kapandi, sonuclar islenip yeni kart olustu.";
        currentPage = KuponPage.Results;
        Refresh();
    }

    private void OnPlaceBet(int index, FutbolKuponManager.MatchResult selection)
    {
        string message;
        if (manager.PlaceBet(index, selection, currentStake, out message))
        {
            statusText.text = message;
        }
        else
        {
            statusText.text = "Bahis atilamadi: " + message;
        }

        Refresh();
    }

    private void ShowOverviewPage()
    {
        currentPage = KuponPage.Overview;
        ApplyPageVisibility();
    }

    private void ShowTodayPage()
    {
        currentPage = KuponPage.Today;
        ApplyPageVisibility();
    }

    private void ShowTomorrowPage()
    {
        currentPage = KuponPage.Tomorrow;
        ApplyPageVisibility();
    }

    private void ShowResultsPage()
    {
        currentPage = KuponPage.Results;
        ApplyPageVisibility();
    }

    private void ApplyPageVisibility()
    {
        if (overviewPageRoot != null)
        {
            overviewPageRoot.gameObject.SetActive(currentPage == KuponPage.Overview);
        }

        if (todayPageRoot != null)
        {
            todayPageRoot.gameObject.SetActive(currentPage == KuponPage.Today);
        }

        if (tomorrowPageRoot != null)
        {
            tomorrowPageRoot.gameObject.SetActive(currentPage == KuponPage.Tomorrow);
        }

        if (resultsPageRoot != null)
        {
            resultsPageRoot.gameObject.SetActive(currentPage == KuponPage.Results);
        }

        SetButtonColor(overviewTabButton, currentPage == KuponPage.Overview);
        SetButtonColor(todayTabButton, currentPage == KuponPage.Today);
        SetButtonColor(tomorrowTabButton, currentPage == KuponPage.Tomorrow);
        SetButtonColor(resultsTabButton, currentPage == KuponPage.Results);
    }

    private void SetButtonColor(Button button, bool active)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = active
                ? new Color(0.62f, 0.39f, 0.12f, 1f)
                : new Color(0.23f, 0.49f, 0.32f, 1f);
        }
    }

    private void EnsurePanelStyle(RectTransform rectTransform)
    {
        Image background = GetOrAddComponent<Image>(rectTransform.gameObject);
        background.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);

        VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(rectTransform.gameObject);
        layout.spacing = 8f;
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(rectTransform.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private MatchRowUi EnsureMatchRow(string rowName, Transform parent, bool interactive)
    {
        MatchRowUi row = new MatchRowUi();
        row.root = EnsureBox(rowName, parent);
        row.titleText = EnsureText("Title", row.root, 16, FontStyles.Bold);
        row.oddsText = EnsureText("Odds", row.root, 14, FontStyles.Normal);
        row.infoText = EnsureText("Info", row.root, 12, FontStyles.Italic);

        if (interactive)
        {
            RectTransform buttonRow = EnsureRow("Buttons", row.root);
            row.homeButton = EnsureButton("Home", buttonRow, "1");
            row.drawButton = EnsureButton("Draw", buttonRow, "X");
            row.awayButton = EnsureButton("Away", buttonRow, "2");
        }

        return row;
    }

    private void EnsureSectionLabel(string name, Transform parent, string value)
    {
        TextMeshProUGUI label = EnsureText(name, parent, 22, FontStyles.Bold);
        label.text = value;
    }

    private RectTransform EnsureBox(string name, Transform parent)
    {
        RectTransform rect = EnsureUiObject(name, parent);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

        VerticalLayoutGroup layout = GetOrAddComponent<VerticalLayoutGroup>(rect.gameObject);
        layout.spacing = 4f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(rect.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    private RectTransform EnsureRow(string name, Transform parent)
    {
        RectTransform rect = EnsureUiObject(name, parent);

        HorizontalLayoutGroup layout = GetOrAddComponent<HorizontalLayoutGroup>(rect.gameObject);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        ContentSizeFitter fitter = GetOrAddComponent<ContentSizeFitter>(rect.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    private TextMeshProUGUI EnsureText(string name, Transform parent, float fontSize, FontStyles style)
    {
        RectTransform rect = EnsureUiObject(name, parent);
        TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
        text.font = GetFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.text = name;
        text.margin = Vector4.zero;
        text.enableWordWrapping = false;
        return text;
    }

    private Button EnsureButton(string name, Transform parent, string label)
    {
        RectTransform rect = EnsureUiObject(name, parent);
        rect.sizeDelta = new Vector2(80f, 28f);

        Image image = GetOrAddComponent<Image>(rect.gameObject);
        image.color = new Color(0.23f, 0.49f, 0.32f, 1f);

        Button button = GetOrAddComponent<Button>(rect.gameObject);
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.3f, 0.58f, 0.37f, 1f);
        colors.pressedColor = new Color(0.16f, 0.38f, 0.23f, 1f);
        button.colors = colors;

        RectTransform labelRect = EnsureUiObject("Label", rect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText = GetOrAddComponent<TextMeshProUGUI>(labelRect.gameObject);
        buttonText.font = GetFont();
        buttonText.fontSize = 14f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.text = label;

        return button;
    }

    private RectTransform EnsureUiObject(string name, Transform parent)
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

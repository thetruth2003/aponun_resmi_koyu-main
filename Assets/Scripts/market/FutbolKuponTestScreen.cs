using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Futbol kuponu panelindeki sayfalari, oran secimlerini ve kupon yatirma arayuzunu yonetir.
/// </summary>
public class FutbolKuponTestScreen : MonoBehaviour
{
    /// <summary>
    /// KuponPage sinifi, pazar ve ekonomi akislarinda kullanilan ilgili davranisi yonetir.
    /// </summary>
    private enum KuponPage
    {
        TicketSummary,
        TodayMatches,
        TomorrowMatches,
        YesterdayResults
    }

    [Serializable]
    /// <summary>
    /// Tek bir mac satirinda kullanilan yazi ve buton referanslarini bir arada tutar.
    /// </summary>
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
    [SerializeField] private Muhasebeci muhasebeci;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI stakeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button placeCouponButton;
    [SerializeField] private Button minusStakeButton;
    [SerializeField] private Button plusStakeButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private RectTransform ticketSummaryPageRoot;
    [SerializeField] private RectTransform todayPageRoot;
    [SerializeField] private RectTransform tomorrowPageRoot;
    [SerializeField] private RectTransform resultsPageRoot;
    [SerializeField] private TextMeshProUGUI ticketSummaryText;
    [SerializeField] private MatchRowUi[] todayRows = new MatchRowUi[2];
    [SerializeField] private MatchRowUi[] tomorrowRows = new MatchRowUi[2];

    private int currentStake = 100;
    private bool listenersBound;
    private KuponPage currentPage = KuponPage.TicketSummary;

    private void OnEnable()
    {
        EnsureManagerAndMoney();
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
            manager.OnStateChanged -= Refresh;

        UnbindListeners();
    }

    private void OnValidate()
    {
        currentStake = Mathf.Max(50, currentStake);
        EnsureManagerAndMoney();
        Refresh();
    }

    private void EnsureManagerAndMoney()
    {
        if (manager == null)
            manager = FindFirstObjectByType<FutbolKuponManager>();

        if (muhasebeci == null && GameManager.instance != null)
            muhasebeci = GameManager.instance.GetComponent<Muhasebeci>();

        if (muhasebeci == null)
            muhasebeci = FindFirstObjectByType<Muhasebeci>();

        if (Application.isPlaying && manager != null)
            manager.ForceRefresh();
    }

    private void BindListeners()
    {
        if (listenersBound)
            UnbindListeners();

        if (minusStakeButton != null) minusStakeButton.onClick.AddListener(OnMinusStake);
        if (plusStakeButton != null) plusStakeButton.onClick.AddListener(OnPlusStake);
        if (placeCouponButton != null) placeCouponButton.onClick.AddListener(OnPlaceCoupon);
        if (previousPageButton != null) previousPageButton.onClick.AddListener(ShowPreviousPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(ShowNextPage);

        for (int i = 0; i < todayRows.Length; i++)
        {
            MatchRowUi row = todayRows[i];
            if (row == null) continue;

            int index = i;
            if (row.homeButton != null) row.homeButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.HomeWin));
            if (row.drawButton != null) row.drawButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.Draw));
            if (row.awayButton != null) row.awayButton.onClick.AddListener(() => OnPlaceBet(index, FutbolKuponManager.MatchResult.AwayWin));
        }

        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (minusStakeButton != null) minusStakeButton.onClick.RemoveAllListeners();
        if (plusStakeButton != null) plusStakeButton.onClick.RemoveAllListeners();
        if (placeCouponButton != null) placeCouponButton.onClick.RemoveAllListeners();
        if (previousPageButton != null) previousPageButton.onClick.RemoveAllListeners();
        if (nextPageButton != null) nextPageButton.onClick.RemoveAllListeners();

        for (int i = 0; i < todayRows.Length; i++)
        {
            MatchRowUi row = todayRows[i];
            if (row == null) continue;
            if (row.homeButton != null) row.homeButton.onClick.RemoveAllListeners();
            if (row.drawButton != null) row.drawButton.onClick.RemoveAllListeners();
            if (row.awayButton != null) row.awayButton.onClick.RemoveAllListeners();
        }

        listenersBound = false;
    }

    private void Refresh()
    {
        if (headerText == null)
            return;

        if (manager == null)
        {
            headerText.text = "Kahve Kuponu";
            if (moneyText != null) moneyText.text = "Manager bulunamadi.";
            return;
        }

        if (!Application.isPlaying)
        {
            headerText.text = "Kahve Kuponu";
            if (moneyText != null) moneyText.text = muhasebeci != null ? $"Para: {muhasebeci.GetMoney()} TL" : "Para: Muhasebeci bagla";
            if (stakeText != null) stakeText.text = currentStake + " TL";
            if (statusText != null) statusText.text = "Play Mode'da guncel veriler dolar.";
            if (resultText != null) resultText.text = "Sonuclar burada gorunur.";
            if (ticketSummaryText != null) ticketSummaryText.text = "Hazir kupon bilgileri burada gorunur.";
            ApplyPageVisibility();
            return;
        }

        headerText.text = "Kahve Kuponu | Gun " + manager.CurrentDay;
        if (moneyText != null) moneyText.text = "Para: " + (muhasebeci != null ? muhasebeci.GetMoney() : manager.GetCurrentMoney()) + " TL";
        if (stakeText != null) stakeText.text = currentStake + " TL";
        if (statusText != null && string.IsNullOrWhiteSpace(statusText.text))
            statusText.text = "Maclari sec, sonra kuponu yatir.";

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

        if (resultText != null)
            resultText.text = string.IsNullOrWhiteSpace(manager.LatestResultsSummary) ? "Henuz gun sonu yok." : manager.LatestResultsSummary;

        if (ticketSummaryText != null)
        {
            ticketSummaryText.text =
                "Bugunun Kupon Durumu\n" +
                manager.GetOpenTicketSummary() +
                "\n\nNot:\n- 2 mac uretilir\n- Oranlar takim gucu ve illegal etkiye gore hesaplanir\n- Gece 00:00 olunca kuponlar otomatik sonuclanir";
        }

        ApplyPageVisibility();
    }

    private void SetRowData(MatchRowUi row, FutbolKuponManager.MatchCard match, bool visible, bool interactive, int rowIndex)
    {
        if (row == null || row.root == null)
            return;

        row.root.gameObject.SetActive(visible);
        if (!visible || match == null)
            return;

        row.titleText.text = match.homeTeam + "  vs  " + match.awayTeam;
        row.oddsText.text = manager.FormatOdds(match);
        row.infoText.text = interactive
            ? manager.GetTicketSummaryForTodayMatch(rowIndex)
            : (match.illegalInfluenceUsed ? "Illegal suphe: " + match.illegalFavoredTeam : "Illegal etki yok");

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

    private void OnPlaceBet(int index, FutbolKuponManager.MatchResult selection)
    {
        if (manager == null) return;
        manager.SetPendingSelection(index, selection);
        if (statusText != null) statusText.text = $"Mac {index + 1} icin secim yapildi: {GetSelectionLabel(selection)}";
        Refresh();
    }

    private void OnPlaceCoupon()
    {
        if (manager == null) return;

        string message;
        if (manager.PlaceCurrentCoupon(currentStake, out message))
        {
            if (statusText != null) statusText.text = message;
        }
        else if (statusText != null)
        {
            statusText.text = "Kupon yatirilamadi: " + message;
        }

        Refresh();
    }

    private void ShowPreviousPage()
    {
        int pageCount = Enum.GetValues(typeof(KuponPage)).Length;
        currentPage = (KuponPage)(((int)currentPage - 1 + pageCount) % pageCount);
        ApplyPageVisibility();
    }

    private void ShowNextPage()
    {
        int pageCount = Enum.GetValues(typeof(KuponPage)).Length;
        currentPage = (KuponPage)(((int)currentPage + 1) % pageCount);
        ApplyPageVisibility();
    }

    private void ApplyPageVisibility()
    {
        if (ticketSummaryPageRoot != null) ticketSummaryPageRoot.gameObject.SetActive(currentPage == KuponPage.TicketSummary);
        if (todayPageRoot != null) todayPageRoot.gameObject.SetActive(currentPage == KuponPage.TodayMatches);
        if (tomorrowPageRoot != null) tomorrowPageRoot.gameObject.SetActive(currentPage == KuponPage.TomorrowMatches);
        if (resultsPageRoot != null) resultsPageRoot.gameObject.SetActive(currentPage == KuponPage.YesterdayResults);
        if (pageIndicatorText != null) pageIndicatorText.text = GetPageLabel(currentPage);
    }

    private string GetPageLabel(KuponPage page)
    {
        return page switch
        {
            KuponPage.TicketSummary => "1/4 | Kupon Ozeti",
            KuponPage.TodayMatches => "2/4 | Bugunun Maclari",
            KuponPage.TomorrowMatches => "3/4 | Yarinin Maclari",
            KuponPage.YesterdayResults => "4/4 | Dunku Sonuclar",
            _ => "Sayfa"
        };
    }

    private string GetSelectionLabel(FutbolKuponManager.MatchResult selection)
    {
        return selection switch
        {
            FutbolKuponManager.MatchResult.HomeWin => "1",
            FutbolKuponManager.MatchResult.Draw => "X",
            FutbolKuponManager.MatchResult.AwayWin => "2",
            _ => "?"
        };
    }

}

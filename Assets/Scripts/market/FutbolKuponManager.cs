using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
/// <summary>
/// Gunluk futbol maclarini ureten, kuponlari tutan ve gun degisiminde kupon sonuclarini hesaplayan yoneticidir.
/// </summary>
public class FutbolKuponManager : MonoBehaviour
{
    private const string DayCountKey = "DayCount";

    /// <summary>
    /// MatchResult sinifi, pazar ve ekonomi akislarinda kullanilan ilgili davranisi yonetir.
    /// </summary>
    public enum MatchResult
    {
        HomeWin,
        Draw,
        AwayWin
    }

    [Serializable]
    /// <summary>
    /// Takim gucu ve illegal etki ihtimali gibi mac hesaplarinda kullanilan temel takim verisini tutar.
    /// </summary>
    public class TeamPower
    {
        public string teamName;
        [Range(20, 100)] public int power = 50;
        [Range(0f, 1f)] public float illegalInfluenceChance = 0.08f;
        [Range(0f, 30f)] public float illegalInfluenceBoost = 8f;
    }

    [Serializable]
    /// <summary>
    /// Bir gun icin uretilen macin oran, olasilik ve sonuc bilgisini saklar.
    /// </summary>
    public class MatchCard
    {
        public int day;
        public string homeTeam;
        public string awayTeam;
        public float homeOdds;
        public float drawOdds;
        public float awayOdds;
        public float homeWinChance;
        public float drawChance;
        public float awayWinChance;
        public bool illegalInfluenceUsed;
        public string illegalFavoredTeam;
        public float illegalSwing;
        public bool resolved;
        public MatchResult result;
    }

    [Serializable]
    /// <summary>
    /// Kupondaki tek bir mac secimini ve o anda kilitlenen orani tutar.
    /// </summary>
    public class CouponLeg
    {
        public int matchIndex;
        public string homeTeam;
        public string awayTeam;
        public MatchResult selection;
        public float lockedOdds;
    }

    [Serializable]
    /// <summary>
    /// Bir gun icin yatirilan tum kuponu, yatirilan tutari ve toplam oraniyla birlikte saklar.
    /// </summary>
    public class DailyCoupon
    {
        public int day;
        public int stake;
        public float totalOdds;
        public List<CouponLeg> legs = new List<CouponLeg>();
    }

    [SerializeField] private List<TeamPower> teams = new List<TeamPower>();
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float houseEdge = 0.92f;
    [SerializeField] private int matchesPerDay = 2;
    [SerializeField] private List<MatchCard> todayMatches = new List<MatchCard>();
    [SerializeField] private List<MatchCard> tomorrowMatches = new List<MatchCard>();
    [SerializeField] private List<DailyCoupon> openCoupons = new List<DailyCoupon>();
    [SerializeField] private List<CouponLeg> pendingCouponLegs = new List<CouponLeg>();
    [SerializeField] private string latestResultsSummary = "";

    private Muhasebeci muhasebeci;
    private bool initialized;

    public event Action OnStateChanged;

    public int CurrentDay => currentDay;
    public IReadOnlyList<MatchCard> TodayMatches => todayMatches;
    public IReadOnlyList<MatchCard> TomorrowMatches => tomorrowMatches;
    public string LatestResultsSummary => latestResultsSummary;

    private void Awake()
    {
        CacheMuhasebeci();
        EnsureDefaults();
        SyncWithWorldDay(false);
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        game_start.OnDayChanged -= HandleWorldDayChanged;
        game_start.OnDayChanged += HandleWorldDayChanged;
    }

    private void OnDisable()
    {
        game_start.OnDayChanged -= HandleWorldDayChanged;
    }

    private void OnValidate()
    {
        EnsureDefaults();
    }

    public void ForceRefresh()
    {
        CacheMuhasebeci();
        EnsureDefaults();
        SyncWithWorldDay(false);
        InitializeIfNeeded();
        NotifyStateChanged();
    }

    public int GetCurrentMoney()
    {
        return muhasebeci != null ? muhasebeci.GetMoney() : 0;
    }

    public void SetPendingSelection(int matchIndex, MatchResult selection)
    {
        if (matchIndex < 0 || matchIndex >= todayMatches.Count)
        {
            return;
        }

        MatchCard match = todayMatches[matchIndex];
        CouponLeg leg = new CouponLeg
        {
            matchIndex = matchIndex,
            homeTeam = match.homeTeam,
            awayTeam = match.awayTeam,
            selection = selection,
            lockedOdds = GetOddsForSelection(match, selection)
        };

        int existingIndex = pendingCouponLegs.FindIndex(x => x.matchIndex == matchIndex);
        if (existingIndex >= 0) pendingCouponLegs[existingIndex] = leg;
        else pendingCouponLegs.Add(leg);

        NotifyStateChanged();
    }

    public bool PlaceCurrentCoupon(int stake, out string message)
    {
        message = string.Empty;

        if (muhasebeci == null)
        {
            message = "Muhasebeci bulunamadi.";
            return false;
        }

        if (stake <= 0)
        {
            message = "Kupon miktari sifirdan buyuk olmali.";
            return false;
        }

        if (pendingCouponLegs.Count != todayMatches.Count || todayMatches.Count == 0)
        {
            message = "Kuponu yatirmak icin tum maclara secim yapmalisin.";
            return false;
        }

        int currentMoney = muhasebeci.GetMoney();
        if (currentMoney < stake)
        {
            message = "Yeterli para yok.";
            return false;
        }

        muhasebeci.SetMoney(currentMoney - stake);

        float totalOdds = 1f;
        DailyCoupon coupon = new DailyCoupon
        {
            day = currentDay,
            stake = stake
        };

        for (int i = 0; i < pendingCouponLegs.Count; i++)
        {
            CouponLeg leg = pendingCouponLegs[i];
            totalOdds *= leg.lockedOdds;
            coupon.legs.Add(new CouponLeg
            {
                matchIndex = leg.matchIndex,
                homeTeam = leg.homeTeam,
                awayTeam = leg.awayTeam,
                selection = leg.selection,
                lockedOdds = leg.lockedOdds
            });
        }

        coupon.totalOdds = totalOdds;
        openCoupons.Add(coupon);
        pendingCouponLegs.Clear();
        message = $"Kupon yatirildi. {stake} TL | Toplam oran {totalOdds:0.00}";
        NotifyStateChanged();
        return true;
    }

    public void AdvanceDay()
    {
        if (todayMatches.Count == 0)
        {
            InitializeIfNeeded();
        }

        ResolveTodayMatches();
        SettleOpenCoupons();

        currentDay++;
        todayMatches = CloneMatches(tomorrowMatches);
        tomorrowMatches = GenerateMatchSet(currentDay + 1);
        pendingCouponLegs.Clear();
        NotifyStateChanged();
    }

    public string GetTicketSummaryForTodayMatch(int matchIndex)
    {
        CouponLeg pending = pendingCouponLegs.Find(x => x.matchIndex == matchIndex);
        if (pending != null)
        {
            return $"Secim: {GetSelectionLabel(pending.selection)} | Oran: {pending.lockedOdds:0.00}";
        }

        int couponCount = 0;
        for (int i = 0; i < openCoupons.Count; i++)
        {
            DailyCoupon coupon = openCoupons[i];
            if (coupon.day == currentDay && coupon.legs.Exists(x => x.matchIndex == matchIndex))
            {
                couponCount++;
            }
        }

        return couponCount > 0 ? $"{couponCount} acik kuponda secildi." : "Henuz secim yok.";
    }

    public string GetOpenTicketSummary()
    {
        List<string> lines = new List<string>();
        int todayCouponCount = 0;
        int totalStake = 0;

        if (pendingCouponLegs.Count > 0)
        {
            float pendingOdds = 1f;
            lines.Add("Hazirlanan kupon:");
            for (int i = 0; i < pendingCouponLegs.Count; i++)
            {
                CouponLeg leg = pendingCouponLegs[i];
                pendingOdds *= leg.lockedOdds;
                lines.Add($"{leg.homeTeam} - {leg.awayTeam} | {GetSelectionLabel(leg.selection)} | {leg.lockedOdds:0.00}");
            }

            lines.Add($"Hazir oran: {pendingOdds:0.00}");
            lines.Add(string.Empty);
        }

        for (int i = 0; i < openCoupons.Count; i++)
        {
            DailyCoupon coupon = openCoupons[i];
            if (coupon.day != currentDay)
            {
                continue;
            }

            todayCouponCount++;
            totalStake += coupon.stake;
            lines.Add($"Yatirilan kupon {todayCouponCount}: {coupon.stake} TL | Oran {coupon.totalOdds:0.00}");
            for (int legIndex = 0; legIndex < coupon.legs.Count; legIndex++)
            {
                CouponLeg leg = coupon.legs[legIndex];
                lines.Add($"- {leg.homeTeam} - {leg.awayTeam} | {GetSelectionLabel(leg.selection)} | {leg.lockedOdds:0.00}");
            }
            lines.Add(string.Empty);
        }

        if (todayCouponCount == 0 && pendingCouponLegs.Count == 0)
        {
            return "Bugun acik ya da hazirlanan kupon yok.";
        }

        lines.Insert(0, $"Toplam risk: {totalStake} TL");
        lines.Insert(0, $"Acik kupon: {todayCouponCount}");
        return string.Join("\n", lines);
    }

    public string FormatOdds(MatchCard match)
    {
        return $"1: {match.homeOdds:0.00}   X: {match.drawOdds:0.00}   2: {match.awayOdds:0.00}";
    }

    public string GetResultLabel(MatchCard match)
    {
        if (!match.resolved) return "Sonuc bekleniyor.";

        return match.result switch
        {
            MatchResult.HomeWin => $"{match.homeTeam} kazandi",
            MatchResult.Draw => "Mac berabere bitti",
            MatchResult.AwayWin => $"{match.awayTeam} kazandi",
            _ => "Sonuc yok"
        };
    }

    private void HandleWorldDayChanged()
    {
        SyncWithWorldDay(true);
    }

    private void SyncWithWorldDay(bool settleIfAdvanced)
    {
        int worldDay = PlayerPrefs.GetInt(DayCountKey, currentDay);
        worldDay = Mathf.Max(1, worldDay);

        if (!settleIfAdvanced)
        {
            currentDay = Mathf.Max(currentDay, worldDay);
            return;
        }

        while (currentDay < worldDay)
        {
            AdvanceDay();
        }
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        if (todayMatches.Count == 0)
        {
            todayMatches = GenerateMatchSet(currentDay);
        }

        if (tomorrowMatches.Count == 0)
        {
            tomorrowMatches = GenerateMatchSet(currentDay + 1);
        }

        initialized = true;
        NotifyStateChanged();
    }

    private void ResolveTodayMatches()
    {
        List<string> lines = new List<string>();

        for (int i = 0; i < todayMatches.Count; i++)
        {
            MatchCard match = todayMatches[i];
            if (!match.resolved)
            {
                match.result = RollMatchResult(match.homeWinChance, match.drawChance, match.awayWinChance);
                match.resolved = true;
                todayMatches[i] = match;
            }

            string illegalNote = match.illegalInfluenceUsed
                ? $" | Illegal etki: {match.illegalFavoredTeam} (+{match.illegalSwing:0.0})"
                : string.Empty;

            lines.Add($"{match.homeTeam} - {match.awayTeam}: {GetResultLabel(match)}{illegalNote}");
        }

        latestResultsSummary = string.Join("\n", lines);
    }

    private void SettleOpenCoupons()
    {
        List<DailyCoupon> remaining = new List<DailyCoupon>();
        List<string> resultLines = new List<string>();

        for (int i = 0; i < openCoupons.Count; i++)
        {
            DailyCoupon coupon = openCoupons[i];
            if (coupon.day != currentDay)
            {
                remaining.Add(coupon);
                continue;
            }

            bool allCorrect = true;
            for (int legIndex = 0; legIndex < coupon.legs.Count; legIndex++)
            {
                CouponLeg leg = coupon.legs[legIndex];
                if (leg.matchIndex < 0 || leg.matchIndex >= todayMatches.Count)
                {
                    allCorrect = false;
                    break;
                }

                MatchCard match = todayMatches[leg.matchIndex];
                if (!match.resolved || leg.selection != match.result)
                {
                    allCorrect = false;
                    break;
                }
            }

            if (allCorrect && muhasebeci != null)
            {
                int winAmount = Mathf.RoundToInt(coupon.stake * coupon.totalOdds);
                muhasebeci.AddMoney(winAmount);
                resultLines.Add($"KAZANAN KUPON: {coupon.stake} TL x {coupon.totalOdds:0.00} = +{winAmount} TL");
            }
            else
            {
                resultLines.Add($"KAYBEDEN KUPON: -{coupon.stake} TL");
            }
        }

        openCoupons = remaining;

        if (resultLines.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(latestResultsSummary))
            {
                latestResultsSummary += "\n\n";
            }

            latestResultsSummary += string.Join("\n", resultLines);
        }
    }

    private List<MatchCard> GenerateMatchSet(int targetDay)
    {
        List<MatchCard> result = new List<MatchCard>();
        List<TeamPower> availableTeams = new List<TeamPower>(teams);
        Shuffle(availableTeams);

        int pairCount = Mathf.Min(matchesPerDay, availableTeams.Count / 2);
        for (int i = 0; i < pairCount; i++)
        {
            TeamPower home = availableTeams[i * 2];
            TeamPower away = availableTeams[i * 2 + 1];
            result.Add(CreateMatchCard(targetDay, home, away));
        }

        return result;
    }

    private MatchCard CreateMatchCard(int day, TeamPower home, TeamPower away)
    {
        float homeBoost = 0f;
        float awayBoost = 0f;
        bool illegalUsed = false;
        string illegalFavoredTeam = string.Empty;
        float illegalSwing = 0f;

        float totalIllegalChance = Mathf.Clamp01(home.illegalInfluenceChance + away.illegalInfluenceChance);
        if (UnityEngine.Random.value < totalIllegalChance)
        {
            illegalUsed = true;
            bool favorHome = UnityEngine.Random.value < GetWeight(home.illegalInfluenceChance, away.illegalInfluenceChance);
            TeamPower favored = favorHome ? home : away;
            illegalSwing = favored.illegalInfluenceBoost * UnityEngine.Random.Range(0.65f, 1.35f);
            illegalFavoredTeam = favored.teamName;

            if (favorHome) homeBoost = illegalSwing;
            else awayBoost = illegalSwing;
        }

        float effectiveHome = home.power + homeBoost;
        float effectiveAway = away.power + awayBoost;
        float diff = effectiveHome - effectiveAway;

        float drawChance = Mathf.Clamp(0.24f - Mathf.Abs(diff) * 0.0022f, 0.08f, 0.24f);
        float homeRaw = Mathf.Exp(diff / 18f);
        float awayRaw = Mathf.Exp(-diff / 18f);
        float remainder = 1f - drawChance;
        float homeChance = remainder * (homeRaw / (homeRaw + awayRaw));
        float awayChance = remainder * (awayRaw / (homeRaw + awayRaw));

        return new MatchCard
        {
            day = day,
            homeTeam = home.teamName,
            awayTeam = away.teamName,
            homeWinChance = homeChance,
            drawChance = drawChance,
            awayWinChance = awayChance,
            homeOdds = CalculateOdds(homeChance),
            drawOdds = CalculateOdds(drawChance),
            awayOdds = CalculateOdds(awayChance),
            illegalInfluenceUsed = illegalUsed,
            illegalFavoredTeam = illegalFavoredTeam,
            illegalSwing = illegalSwing,
            resolved = false
        };
    }

    private float CalculateOdds(float probability)
    {
        probability = Mathf.Clamp(probability, 0.05f, 0.9f);
        return Mathf.Max(1.15f, houseEdge / probability);
    }

    private MatchResult RollMatchResult(float homeChance, float drawChance, float awayChance)
    {
        float roll = UnityEngine.Random.value;
        if (roll <= homeChance) return MatchResult.HomeWin;
        if (roll <= homeChance + drawChance) return MatchResult.Draw;
        return MatchResult.AwayWin;
    }

    private float GetOddsForSelection(MatchCard match, MatchResult selection)
    {
        return selection switch
        {
            MatchResult.HomeWin => match.homeOdds,
            MatchResult.Draw => match.drawOdds,
            MatchResult.AwayWin => match.awayOdds,
            _ => 1f
        };
    }

    private string GetSelectionLabel(MatchResult selection)
    {
        return selection switch
        {
            MatchResult.HomeWin => "1",
            MatchResult.Draw => "X",
            MatchResult.AwayWin => "2",
            _ => "?"
        };
    }

    private void CacheMuhasebeci()
    {
        if (GameManager.instance != null)
        {
            muhasebeci = GameManager.instance.GetComponent<Muhasebeci>();
        }

        if (muhasebeci == null)
        {
            muhasebeci = FindFirstObjectByType<Muhasebeci>();
        }
    }

    private List<MatchCard> CloneMatches(List<MatchCard> source)
    {
        List<MatchCard> clones = new List<MatchCard>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            MatchCard s = source[i];
            clones.Add(new MatchCard
            {
                day = s.day,
                homeTeam = s.homeTeam,
                awayTeam = s.awayTeam,
                homeOdds = s.homeOdds,
                drawOdds = s.drawOdds,
                awayOdds = s.awayOdds,
                homeWinChance = s.homeWinChance,
                drawChance = s.drawChance,
                awayWinChance = s.awayWinChance,
                illegalInfluenceUsed = s.illegalInfluenceUsed,
                illegalFavoredTeam = s.illegalFavoredTeam,
                illegalSwing = s.illegalSwing,
                resolved = s.resolved,
                result = s.result
            });
        }

        return clones;
    }

    private void EnsureDefaults()
    {
        if (teams == null)
        {
            teams = new List<TeamPower>();
        }

        if (teams.Count > 0)
        {
            return;
        }

        teams.Add(new TeamPower { teamName = "Koyspor", power = 74, illegalInfluenceChance = 0.04f, illegalInfluenceBoost = 4f });
        teams.Add(new TeamPower { teamName = "Ankara Birlik", power = 82, illegalInfluenceChance = 0.11f, illegalInfluenceBoost = 12f });
        teams.Add(new TeamPower { teamName = "Recep FK", power = 79, illegalInfluenceChance = 0.24f, illegalInfluenceBoost = 18f });
        teams.Add(new TeamPower { teamName = "Caykur Derman", power = 68, illegalInfluenceChance = 0.08f, illegalInfluenceBoost = 7f });
        teams.Add(new TeamPower { teamName = "Tekelgucu", power = 65, illegalInfluenceChance = 0.05f, illegalInfluenceBoost = 5f });
        teams.Add(new TeamPower { teamName = "Muhtarlik SK", power = 71, illegalInfluenceChance = 0.18f, illegalInfluenceBoost = 14f });
        teams.Add(new TeamPower { teamName = "Ogretmenler Birligi", power = 76, illegalInfluenceChance = 0.03f, illegalInfluenceBoost = 3f });
        teams.Add(new TeamPower { teamName = "Tamirhane 61", power = 69, illegalInfluenceChance = 0.06f, illegalInfluenceBoost = 6f });
    }

    private float GetWeight(float left, float right)
    {
        float total = Mathf.Max(0.0001f, left + right);
        return left / total;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}

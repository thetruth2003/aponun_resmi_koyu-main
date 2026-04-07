using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class FutbolKuponManager : MonoBehaviour
{
    public enum MatchResult
    {
        HomeWin,
        Draw,
        AwayWin
    }

    [Serializable]
    public class TeamPower
    {
        public string teamName;
        [Range(20, 100)] public int power = 50;
        [Range(0f, 1f)] public float illegalInfluenceChance = 0.08f;
        [Range(0f, 30f)] public float illegalInfluenceBoost = 8f;
    }

    [Serializable]
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
    public class BetTicket
    {
        public int day;
        public int matchIndex;
        public string homeTeam;
        public string awayTeam;
        public MatchResult selection;
        public int stake;
        public float lockedOdds;
    }

    [Header("Team Pool")]
    [SerializeField] private List<TeamPower> teams = new List<TeamPower>();

    [Header("Simulation")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private float houseEdge = 0.92f;
    [SerializeField] private int matchesPerDay = 2;
    [SerializeField] private int demoStartingMoney = 5000;

    [Header("State")]
    [SerializeField] private List<MatchCard> todayMatches = new List<MatchCard>();
    [SerializeField] private List<MatchCard> tomorrowMatches = new List<MatchCard>();
    [SerializeField] private List<BetTicket> openTickets = new List<BetTicket>();
    [SerializeField] private string latestResultsSummary = "";

    private Muhasebeci muhasebeci;
    private Money legacyMoney;
    private int fallbackMoney;
    private bool initialized;

    public event Action OnStateChanged;

    public int CurrentDay => currentDay;
    public IReadOnlyList<MatchCard> TodayMatches => todayMatches;
    public IReadOnlyList<MatchCard> TomorrowMatches => tomorrowMatches;
    public string LatestResultsSummary => latestResultsSummary;

    private void Awake()
    {
        CacheMoneySystems();
        EnsureDefaults();
        InitializeIfNeeded();
    }

    private void OnValidate()
    {
        EnsureDefaults();
    }

    public void ForceRefresh()
    {
        CacheMoneySystems();
        EnsureDefaults();
        InitializeIfNeeded();
        NotifyStateChanged();
    }

    public int GetCurrentMoney()
    {
        if (muhasebeci != null)
        {
            return muhasebeci.GetMoney();
        }

        if (legacyMoney != null)
        {
            return legacyMoney.currentMoney;
        }

        return fallbackMoney;
    }

    public bool PlaceBet(int matchIndex, MatchResult selection, int stake, out string message)
    {
        message = string.Empty;

        if (stake <= 0)
        {
            message = "Miktar sifirdan buyuk olmali.";
            return false;
        }

        if (matchIndex < 0 || matchIndex >= todayMatches.Count)
        {
            message = "Mac bulunamadi.";
            return false;
        }

        MatchCard match = todayMatches[matchIndex];
        if (match.resolved)
        {
            message = "Bu mac zaten sonuclanmis.";
            return false;
        }

        if (!TrySpendMoney(stake))
        {
            message = "Yeterli para yok.";
            return false;
        }

        openTickets.Add(new BetTicket
        {
            day = currentDay,
            matchIndex = matchIndex,
            homeTeam = match.homeTeam,
            awayTeam = match.awayTeam,
            selection = selection,
            stake = stake,
            lockedOdds = GetOddsForSelection(match, selection)
        });

        message = $"{match.homeTeam} - {match.awayTeam} macina bahis yapildi.";
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
        SettleOpenTickets();

        currentDay++;
        todayMatches = CloneMatches(tomorrowMatches);
        tomorrowMatches = GenerateMatchSet(currentDay + 1);
        NotifyStateChanged();
    }

    public string GetTicketSummaryForTodayMatch(int matchIndex)
    {
        int ticketCount = 0;
        int totalStake = 0;

        for (int i = 0; i < openTickets.Count; i++)
        {
            if (openTickets[i].day == currentDay && openTickets[i].matchIndex == matchIndex)
            {
                ticketCount++;
                totalStake += openTickets[i].stake;
            }
        }

        if (ticketCount == 0)
        {
            return "Bu maca acik bahis yok.";
        }

        return $"{ticketCount} bahis acik, toplam {totalStake} TL.";
    }

    public string FormatOdds(MatchCard match)
    {
        return $"1: {match.homeOdds:0.00}   X: {match.drawOdds:0.00}   2: {match.awayOdds:0.00}";
    }

    public string GetResultLabel(MatchCard match)
    {
        if (!match.resolved)
        {
            return "Sonuc bekleniyor.";
        }

        return match.result switch
        {
            MatchResult.HomeWin => $"{match.homeTeam} kazandi",
            MatchResult.Draw => "Mac berabere bitti",
            MatchResult.AwayWin => $"{match.awayTeam} kazandi",
            _ => "Sonuc yok"
        };
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

        if (GetCurrentMoney() <= 0)
        {
            ForceSetMoney(demoStartingMoney);
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

    private void SettleOpenTickets()
    {
        List<BetTicket> remaining = new List<BetTicket>();
        List<string> resultLines = new List<string>();

        for (int i = 0; i < openTickets.Count; i++)
        {
            BetTicket ticket = openTickets[i];
            if (ticket.day != currentDay)
            {
                remaining.Add(ticket);
                continue;
            }

            if (ticket.matchIndex < 0 || ticket.matchIndex >= todayMatches.Count)
            {
                continue;
            }

            MatchCard match = todayMatches[ticket.matchIndex];
            if (!match.resolved)
            {
                remaining.Add(ticket);
                continue;
            }

            if (ticket.selection == match.result)
            {
                int winAmount = Mathf.RoundToInt(ticket.stake * ticket.lockedOdds);
                AddMoney(winAmount);
                resultLines.Add($"KAZANAN: {ticket.homeTeam} - {ticket.awayTeam} | +{winAmount} TL");
            }
            else
            {
                resultLines.Add($"KAYBEDEN: {ticket.homeTeam} - {ticket.awayTeam} | -{ticket.stake} TL");
            }
        }

        openTickets = remaining;

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

            if (favorHome)
            {
                homeBoost = illegalSwing;
            }
            else
            {
                awayBoost = illegalSwing;
            }
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
        if (roll <= homeChance)
        {
            return MatchResult.HomeWin;
        }

        if (roll <= homeChance + drawChance)
        {
            return MatchResult.Draw;
        }

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

    private void CacheMoneySystems()
    {
        muhasebeci = FindFirstObjectByType<Muhasebeci>();
        legacyMoney = FindFirstObjectByType<Money>();
    }

    private bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (muhasebeci != null)
        {
            int currentMoney = muhasebeci.GetMoney();
            if (currentMoney < amount)
            {
                return false;
            }

            muhasebeci.SetMoney(currentMoney - amount);
            return true;
        }

        if (legacyMoney != null)
        {
            return legacyMoney.SpendMoney(amount);
        }

        if (fallbackMoney < amount)
        {
            return false;
        }

        fallbackMoney -= amount;
        return true;
    }

    private void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (muhasebeci != null)
        {
            muhasebeci.AddMoney(amount);
            return;
        }

        if (legacyMoney != null)
        {
            legacyMoney.AddMoney(amount);
            return;
        }

        fallbackMoney += amount;
    }

    private void ForceSetMoney(int amount)
    {
        if (muhasebeci != null)
        {
            muhasebeci.SetMoney(amount);
            return;
        }

        if (legacyMoney != null)
        {
            legacyMoney.currentMoney = amount;
            legacyMoney.UpdateMoneyUI();
            return;
        }

        fallbackMoney = amount;
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

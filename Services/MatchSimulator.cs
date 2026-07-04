using LiveSportsTicker.Models;

namespace LiveSportsTicker.Services;

/// <summary>
/// Holds the full history of match events in memory (thread-safe) so that:
///  1) A live SSE connection can stream new events as they happen.
///  2) A reconnecting client (using Last-Event-ID) can catch up on everything it missed.
/// Also implements the "smart streaming idea": a live win-probability recomputation
/// after every single event, based on score difference, minutes remaining and momentum.
/// </summary>
public class MatchSimulator
{
    public const string HomeTeam = "Al-Ittihad";
    public const string AwayTeam = "Al-Nasr";

    private readonly object _lock = new();
    private readonly List<MatchEvent> _events = new();
    private long _nextId = 1;

    public int Minute { get; private set; } = 0;
    public int ScoreHome { get; private set; } = 0;
    public int ScoreAway { get; private set; } = 0;
    public bool MatchOver { get; private set; } = false;

    public MatchSimulator()
    {
        // Seed the stream with a kickoff event so there's always at least one item.
        AddEvent(MatchEventType.KickOff, "", "Kick-off! The match is underway.");
    }

    public IReadOnlyList<MatchEvent> GetAllEvents()
    {
        lock (_lock) return _events.ToList();
    }

    /// <summary>Used to resume a dropped SSE connection: return everything after lastId.</summary>
    public IReadOnlyList<MatchEvent> GetEventsSince(long lastId)
    {
        lock (_lock) return _events.Where(e => e.Id > lastId).ToList();
    }

    public long LatestId
    {
        get { lock (_lock) return _events.Count == 0 ? 0 : _events[^1].Id; }
    }

    public void AdvanceMinute()
    {
        lock (_lock)
        {
            if (MatchOver) return;
            Minute++;

            if (Minute == 45)
                AddEventInternal(MatchEventType.Halftime, "", "Half-time.");

            if (Minute >= 90)
            {
                AddEventInternal(MatchEventType.FullTime, "", "Full-time! The match has ended.");
                MatchOver = true;
            }
        }
    }

    public MatchEvent AddEvent(MatchEventType type, string team, string description)
    {
        lock (_lock)
        {
            return AddEventInternal(type, team, description);
        }
    }

    private MatchEvent AddEventInternal(MatchEventType type, string team, string description)
    {
        if (type == MatchEventType.Goal)
        {
            if (team == HomeTeam) ScoreHome++;
            else if (team == AwayTeam) ScoreAway++;
        }

        var (homeWin, draw, awayWin) = ComputeWinProbability();

        var evt = new MatchEvent
        {
            Id = _nextId++,
            Minute = Minute,
            Type = type,
            Team = team,
            Description = description,
            ScoreHome = ScoreHome,
            ScoreAway = ScoreAway,
            HomeWinProbability = homeWin,
            DrawProbability = draw,
            AwayWinProbability = awayWin
        };

        _events.Add(evt);
        return evt;
    }

    /// <summary>
    /// Very small heuristic model: score difference dominates, weighted more heavily
    /// as the clock runs down (a 1-goal lead in the 88th minute matters more than
    /// in the 5th). Not real ML, but it demonstrates a value being recomputed live
    /// after every streamed event - the "smart" part of this streaming project.
    /// </summary>
    private (double home, double draw, double away) ComputeWinProbability()
    {
        int diff = ScoreHome - ScoreAway;
        double minutesRemaining = Math.Max(90 - Minute, 1);
        double timeWeight = 1.0 + (90.0 - minutesRemaining) / 90.0; // grows from 1.0 -> 2.0

        double homeStrength = 0.50 + diff * 0.18 * timeWeight;
        homeStrength = Math.Clamp(homeStrength, 0.02, 0.96);

        double drawBase = Math.Max(0.28 - Math.Abs(diff) * 0.10, 0.05);
        double remaining = 1.0 - drawBase;

        double home = remaining * homeStrength;
        double away = remaining * (1 - homeStrength);

        double total = home + drawBase + away;
        return (Math.Round(home / total * 100, 1),
                Math.Round(drawBase / total * 100, 1),
                Math.Round(away / total * 100, 1));
    }
}

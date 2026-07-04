namespace LiveSportsTicker.Models;

public enum MatchEventType
{
    KickOff,
    Goal,
    YellowCard,
    RedCard,
    Substitution,
    Halftime,
    FullTime,
    Commentary
}

/// <summary>
/// A single event pushed through the live stream. Id is a monotonically increasing
/// sequence number used both as the SSE "id:" field and to support resuming a
/// dropped connection via the "Last-Event-ID" request header.
/// </summary>
public class MatchEvent
{
    public long Id { get; set; }
    public int Minute { get; set; }
    public MatchEventType Type { get; set; }
    public string Team { get; set; } = "";
    public string Description { get; set; } = "";
    public int ScoreHome { get; set; }
    public int ScoreAway { get; set; }

    // Smart streaming idea: live win-probability recomputed after every event.
    public double HomeWinProbability { get; set; }
    public double DrawProbability { get; set; }
    public double AwayWinProbability { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

using LiveSportsTicker.Models;

namespace LiveSportsTicker.Services;

/// <summary>
/// Runs for the lifetime of the app, ticking the match clock forward and
/// occasionally injecting a random event (goal / card / substitution / commentary).
/// This is what makes the stream "continuous" - events are produced independently
/// of any particular browser connection; the SSE endpoint just reads whatever
/// this service has produced so far, plus anything new as it happens.
/// </summary>
public class MatchClockService : BackgroundService
{
    private readonly MatchSimulator _simulator;
    private readonly Random _rng = new();

    private static readonly string[] Commentary =
    {
        "Good build-up play through midfield.",
        "A dangerous cross is cleared by the defense.",
        "The crowd is really getting behind their team now.",
        "A tactical shift as the coach reacts from the sideline.",
        "Free kick in a promising position.",
        "Great save by the goalkeeper!"
    };

    public MatchClockService(MatchSimulator simulator)
    {
        _simulator = simulator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ~1 simulated minute every 2 seconds -> a full 90-minute match runs in about 3 minutes,
        // fast enough to demo the full lifecycle (kickoff -> halftime -> fulltime) on video.
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            if (_simulator.MatchOver)
            {
                // Keep the service alive but idle once the match has ended.
                continue;
            }

            _simulator.AdvanceMinute();
            MaybeGenerateEvent();
        }
    }

    private void MaybeGenerateEvent()
    {
        if (_simulator.MatchOver) return;

        double roll = _rng.NextDouble();

        if (roll < 0.08)
        {
            string team = _rng.Next(2) == 0 ? MatchSimulator.HomeTeam : MatchSimulator.AwayTeam;
            _simulator.AddEvent(MatchEventType.Goal, team, $"GOAL! {team} score!");
        }
        else if (roll < 0.14)
        {
            string team = _rng.Next(2) == 0 ? MatchSimulator.HomeTeam : MatchSimulator.AwayTeam;
            _simulator.AddEvent(MatchEventType.YellowCard, team, $"Yellow card shown to a {team} player.");
        }
        else if (roll < 0.16)
        {
            string team = _rng.Next(2) == 0 ? MatchSimulator.HomeTeam : MatchSimulator.AwayTeam;
            _simulator.AddEvent(MatchEventType.RedCard, team, $"RED CARD! {team} down to 10 men!");
        }
        else if (roll < 0.22)
        {
            string team = _rng.Next(2) == 0 ? MatchSimulator.HomeTeam : MatchSimulator.AwayTeam;
            _simulator.AddEvent(MatchEventType.Substitution, team, $"{team} make a substitution.");
        }
        else if (roll < 0.40)
        {
            string line = Commentary[_rng.Next(Commentary.Length)];
            _simulator.AddEvent(MatchEventType.Commentary, "", line);
        }
        // else: no event this tick, just the clock ticking (client shows a heartbeat).
    }
}

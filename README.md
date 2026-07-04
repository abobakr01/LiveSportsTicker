<<<<<<< HEAD
# Live Sports Ticker

Course: Advanced Web Development - Streaming (ASP.NET Core MVC)
Student: Full Name

Project #1 from the assignment list: **Live Sports Ticker** — push live match scores
and events to the browser the moment they happen, over a single continuously open
connection.

## Demo Video
https://youtu.be/feKiUBnEUnQ 

## Streaming
- **Technique:** Server-Sent Events (SSE), via a plain `text/event-stream` response
  written and flushed manually in `StreamController.Live()` — no third-party SSE
  library, just ASP.NET Core's `HttpResponse` stream kept open.
- **What is streamed:** A simulated football match. A background service
  (`MatchClockService`) advances the match clock and randomly raises events
  (kick-off, goals, cards, substitutions, half-time, full-time, commentary).
  Every event is appended to an in-memory log (`MatchSimulator`) with an
  incrementing id. The SSE endpoint streams new events as they're produced and,
  on (re)connect, first replays anything the client missed using `Last-Event-ID`
  (or a `?lastEventId=` query fallback for the manual "kill connection" demo
  button), then continues live. Heartbeat comments (`:heartbeat`) are sent every
  second with no news so the connection is known to be alive.
- **Smart idea:** Live win-probability is recomputed after *every single event*
  (score, cards, and time remaining all shift it) and streamed alongside the
  event — shown as a live-updating probability bar in the UI.

## How to run
```
dotnet restore
dotnet run
```
Then open the URL shown in the console (default `http://localhost:5080`). The
match starts automatically and runs in fast-forward (~2 seconds per simulated
minute, so a full 90-minute match plays out in about 3 minutes — enough to
demo kickoff → halftime → fulltime on video).

To demo **reconnection**: click "Kill Connection (simulate drop)" — the stream
closes, the status dot goes red, then after 3 seconds it automatically
reconnects and replays any events that happened while it was down before
resuming live, with no page reload.

## Project structure
```
Controllers/
  HomeController.cs      Serves the Razor page
  StreamController.cs    GET /Stream/Live - the SSE endpoint
Services/
  MatchSimulator.cs       In-memory event log + win-probability model
  MatchClockService.cs    Background service that drives the match clock
Models/
  MatchEvent.cs            One streamed event (score, minute, type, win %)
Views/Home/Index.cshtml    Live UI: EventSource, scoreboard, probability bar, feed
wwwroot/css/site.css       Styling
```

## Submission checklist (per course guide)
- [ ] Record a ≤5 minute screen capture showing the stream running live and a
      reconnect (kill the connection, show it resume).
- [ ] Upload to YouTube (Unlisted) and paste the link above.
- [ ] Push this repo to GitHub with a clean history (no `bin/`, `obj/`, secrets).
- [ ] Submit the GitHub repository link.
=======
# LiveSportsTicker
>>>>>>> 2fc017da7ab90309198d46a3a63b79fdfaad09d7

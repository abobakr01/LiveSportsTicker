using LiveSportsTicker.Services;

var builder = WebApplication.CreateBuilder(args);

// Listen on specific URLs so the app opens on the requested port when launched.
builder.WebHost.UseUrls("http://localhost:5050", "https://localhost:7240");

// MVC (controllers + Razor views)
builder.Services.AddControllersWithViews();

// Singleton match-simulation engine: generates events + keeps history for reconnect/replay
builder.Services.AddSingleton<MatchSimulator>();
// Background service that drives the simulator clock (fires new events over time)
builder.Services.AddHostedService<MatchClockService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

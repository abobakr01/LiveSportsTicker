using LiveSportsTicker.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiveSportsTicker.Controllers;

public class HomeController : Controller
{
    private readonly MatchSimulator _simulator;

    public HomeController(MatchSimulator simulator)
    {
        _simulator = simulator;
    }

    public IActionResult Index()
    {
        ViewBag.HomeTeam = MatchSimulator.HomeTeam;
        ViewBag.AwayTeam = MatchSimulator.AwayTeam;
        return View();
    }

    public IActionResult Error() => View();
}

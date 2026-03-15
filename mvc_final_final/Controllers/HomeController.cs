using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using mvc_final_final.Models;

namespace mvc_final_final.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Donor");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

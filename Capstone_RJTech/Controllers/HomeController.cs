using System.Diagnostics;
using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Dashboard(int? month, int? year, int? categoryId)
            => RedirectToAction("Index", "Dashboard", new { month, year, categoryId });

        public IActionResult Privacy() => View();
        public IActionResult Archive() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

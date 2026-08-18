using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class NotificationController : Controller
    {
        public IActionResult Index() => RedirectToAction(nameof(Notification));
        public IActionResult Notification() => View();
        public IActionResult Calendar() => View();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult Index() => RedirectToAction(nameof(Checkout));
        public IActionResult Checkout() => View();
        public IActionResult Refund() => View();
    }
}

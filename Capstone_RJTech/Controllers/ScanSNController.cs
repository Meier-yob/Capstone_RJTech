using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class ScanSNController : Controller
    {
        public IActionResult Index() => RedirectToAction(nameof(ScanSerialNumber));
        public IActionResult ScanSerialNumber() => View("~/Views/Scan SN/ScanSerialNumber.cshtml");
    }
}

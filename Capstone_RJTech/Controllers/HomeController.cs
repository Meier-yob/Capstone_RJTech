using System.Diagnostics;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Dashboard()
        {
            var products = _db.Products.ToList();
            bool statusChanged = false;
            foreach (var product in products)
            {
                string status = ProductController.EvaluateProductStatus(product);
                if (product.product_status != status)
                {
                    product.product_status = status;
                    statusChanged = true;
                }
            }
            if (statusChanged) _db.SaveChanges();

            ViewBag.TotalProducts = products.Count;
            ViewBag.UnavailableCount = products.Count(product => product.product_status == "Unavailable");
            ViewBag.LowStockCount = products.Count(product => product.product_status == "Low Stock");
            ViewBag.OutOfStockCount = products.Count(product => product.product_status == "Out of Stock");
            ViewBag.TodayDeliveryCount = _db.Deliveries.Count(delivery => delivery.date_delivered.Date == DateTime.Today);
            return View();
        }

        public IActionResult Privacy() => View();
        public IActionResult Archive() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

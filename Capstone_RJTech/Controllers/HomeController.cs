using System.Diagnostics;
using Capstone_RJTech.Models;
using Microsoft.AspNetCore.Mvc;

namespace Capstone_RJTech.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Dashboard()
        {
            foreach (var product in InventoryStore.Products)
                product.product_status = ProductController.EvaluateProductStatus(product);

            ViewBag.TotalProducts = InventoryStore.Products.Count;
            ViewBag.UnavailableCount = InventoryStore.Products.Count(product => product.product_status == "Unavailable");
            ViewBag.LowStockCount = InventoryStore.Products.Count(product => product.product_status == "Low Stock");
            ViewBag.OutOfStockCount = InventoryStore.Products.Count(product => product.product_status == "Out of Stock");
            ViewBag.TodayDeliveryCount = InventoryStore.Deliveries.Count(delivery => delivery.date_delivered.Date == DateTime.Today);
            return View();
        }

        public IActionResult Privacy() => View();
        public IActionResult Archive() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

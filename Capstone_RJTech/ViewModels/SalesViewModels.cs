using Capstone_RJTech.Models;
using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels
{
    public class CheckoutItemRequest
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public List<string> SerialNumbers { get; set; } = new();
    }

    public class SaveCheckoutRequest
    {
        public int? CustomerID { get; set; }

        [Required]
        public string CustomerFullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        [RegularExpression(@"^[^@\s]+@[Gg][Mm][Aa][Ii][Ll]\.[Cc][Oo][Mm]$",
            ErrorMessage = "Email must be a valid @gmail.com address")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required, StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain exactly 11 digits")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string CustomerAddress { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "Cash";
        public List<CheckoutItemRequest> Items { get; set; } = new();
    }

    public class CheckoutFormItemViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public List<string> SerialNumbers { get; set; } = new();
        public decimal Price { get; set; }
        public int AvailableStock { get; set; }
    }

    public class CheckoutFormViewModel
    {
        public int? CustomerID { get; set; }
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "Cash";
        public DateTime DatePurchased { get; set; } = DateTime.Now;
        public List<CheckoutFormItemViewModel> Items { get; set; } = new();
    }

    public class CheckoutDetailsViewModel
    {
        public required Checkout Checkout { get; set; }
    }
}

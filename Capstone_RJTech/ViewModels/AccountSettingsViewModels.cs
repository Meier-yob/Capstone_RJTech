using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels;

public sealed class AccountSettingsViewModel
{
    public EditProfileViewModel Profile { get; set; } = new();
    public ChangePasswordViewModel Security { get; set; } = new();
}

public sealed class EditProfileViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression("^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "Username may only contain letters, numbers, and the characters . _ -")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>Display-only; always "Owner". Not editable.</summary>
    public string Role { get; set; } = "Owner";

    /// <summary>Display-only; read from database.</summary>
    public DateTime DateCreated { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Username or email is required.")]
    [Display(Name = "Username or Email")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public sealed class SignUpViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression("^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "Username may only contain letters, numbers, and the characters . _ -")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    [Display(Name = "Registered Email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class VerifyOtpViewModel
{
    [Required(ErrorMessage = "The verification code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "The code is 6 digits.")]
    [Display(Name = "Verification Code")]
    public string Otp { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "New Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;
}
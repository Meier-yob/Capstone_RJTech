using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels;

public sealed class AcceptInvitationViewModel
{
    public string Token { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}

public sealed class CreateOwnerAccountViewModel
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
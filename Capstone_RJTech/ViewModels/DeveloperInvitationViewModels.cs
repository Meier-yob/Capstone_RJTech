using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.ViewModels;

public sealed class InvitationListViewModel
{
    public IReadOnlyList<DeveloperInvitationViewModel> Invitations { get; init; } =
        Array.Empty<DeveloperInvitationViewModel>();

    public SendInvitationViewModel NewInvitation { get; init; } = new();
}

public sealed class DeveloperInvitationViewModel
{
    public int InvitationID { get; init; }

    public string Email { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class SendInvitationViewModel
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
}

public sealed class InvitationActionViewModel
{
    [Required]
    public int InvitationID { get; set; }
}
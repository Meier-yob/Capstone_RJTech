using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models;

[Table("tblOwnerInvitation")]
public sealed class OwnerInvitation
{
    [Key]
    public int InvitationID { get; set; }

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public OwnerInvitationStatus Status { get; set; } = OwnerInvitationStatus.Pending;

    [Required, StringLength(256)]
    public string CreatedByClerkUserId { get; set; } = string.Empty;
}
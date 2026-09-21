using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models;

/// <summary>
/// One-time password (OTP) issued for the forgot-password flow (table tblPasswordResetCode).
/// Stores only a SHA-256 hash of the OTP, never the code itself.
/// </summary>
[Table("tblPasswordResetCode")]
public sealed class PasswordResetCode
{
    /// <summary>Random nonce that identifies the reset request; carried in the flow URLs.</summary>
    [Key]
    [StringLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string OtpHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set when the user successfully entered the code; required before the password can be reset.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }
}
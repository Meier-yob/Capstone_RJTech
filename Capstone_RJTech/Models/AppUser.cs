using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models;

/// <summary>
/// Application user account (table tblUser). Replaces the old Owner model.
/// The <see cref="Password"/> property stores the PBKDF2-SHA256 hash produced by
/// <see cref="Services.PasswordHashService"/> — never a plaintext password.
/// </summary>
[Table("tblUser")]
public sealed class AppUser
{
    [Key]
    [StringLength(450)]
    public string UserID { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Display-only role label. Every account in this system is an Owner, so this is
    /// always set to "Owner" — it is not used for authorization or access control.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "Owner";

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
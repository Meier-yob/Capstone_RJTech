using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models;

public sealed class Owner
{
    [Key]
    [StringLength(450)]
    public string OwnerID { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string Role { get; set; } = "Owner";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required, EmailAddress, StringLength(256)]
    public string RecoveryEmail { get; set; } = string.Empty;
}

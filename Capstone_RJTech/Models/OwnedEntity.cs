using System.ComponentModel.DataAnnotations;

namespace Capstone_RJTech.Models;

public abstract class OwnedEntity
{
    [Required]
    [StringLength(450)]
    public string OwnerID { get; set; } = string.Empty;

}

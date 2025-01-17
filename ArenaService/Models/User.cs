using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("users")]
[Index(nameof(AgentAddress), IsUnique = true)]
public class User
{
    [Key]
    [StringLength(40, MinimumLength = 40)]
    public required string AvatarAddress { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string AgentAddress { get; set; }

    [Required]
    public required string NameWithHash { get; set; }

    [Required]
    public int PortraitId { get; set; }

    [Required]
    public long Cp { get; set; }

    [Required]
    public int Level { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

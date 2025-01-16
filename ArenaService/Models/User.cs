using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("users")]
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
    [Column(TypeName = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

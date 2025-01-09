using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("users")]
public class User
{
    [Key]
    public required string AvatarAddress { get; set; }

    [Required]
    public required string AgentAddress { get; set; }

    [Required]
    public required string NameWithHash { get; set; }

    [Required]
    public int PortraitId { get; set; }

    [Required]
    public long Cp { get; set; }

    [Required]
    public int Level { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("users")]
public class User
{
    [Key]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AgentAddress { get; set; }

    [Required]
    public required string NameWithHash { get; set; }

    [Required]
    public int PortraitId { get; set; }

    [Required]
    public long Cp { get; set; }

    [Required]
    public int Level { get; set; }

    public int? ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public Clan? Clan { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("medals")]
[PrimaryKey(nameof(AvatarAddress), nameof(SeasonId))]
[Index(nameof(MedalCount), nameof(SeasonId))]
public class Medal
{
    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    [ForeignKey(nameof(AvatarAddress))]
    public User User { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    public int MedalCount { get; set; } = 0;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

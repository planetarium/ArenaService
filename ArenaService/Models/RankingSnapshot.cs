using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("ranking_snapshots")]
[PrimaryKey(nameof(AvatarAddress), nameof(SeasonId), nameof(RoundId))]
[Index(nameof(SeasonId), nameof(RoundId))]
public class RankingSnapshot
{
    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    public int RoundId { get; set; }

    [ForeignKey(nameof(RoundId))]
    public Round Round { get; set; } = null!;

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    public Participant Participant { get; set; } = null!;

    [Required]
    public int Score { get; set; }

    public int? ClanId { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

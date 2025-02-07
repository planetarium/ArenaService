using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models;

[Table("clan_ranking_snapshots")]
[PrimaryKey(nameof(ClanId), nameof(SeasonId), nameof(RoundId))]
[Index(nameof(SeasonId), nameof(RoundId))]
public class ClanRankingSnapshot
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
    public int ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public Clan Clan { get; set; } = null!;

    [Required]
    public int Score { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

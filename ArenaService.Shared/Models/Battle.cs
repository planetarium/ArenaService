using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models;

[Table("battles")]
[Index(nameof(Id), nameof(TxId))]
[Index(nameof(BattleStatus), nameof(Reviewed))]
public class Battle
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    public Participant Participant { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    public int RoundId { get; set; }

    [ForeignKey(nameof(RoundId))]
    public Round Round { get; set; } = null!;

    [Required]
    public int AvailableOpponentId { get; set; }

    [ForeignKey(nameof(AvailableOpponentId))]
    public AvailableOpponent AvailableOpponent { get; set; } = null!;

    [Required]
    public required string Token { get; set; }

    public BattleStatus BattleStatus { get; set; }

    public TxId? TxId { get; set; }

    public TxStatus? TxStatus { get; set; }

    public string? ExceptionNames { get; set; }

    public bool? Reviewed { get; set; }

    public bool? IsVictory { get; set; }

    public int? MyScoreChange { get; set; }

    public int? OpponentScoreChange { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

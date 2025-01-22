using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;
using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Models;

[Table("battles")]
public class Battle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int AvailableOpponentId { get; set; }

    [ForeignKey(nameof(AvailableOpponentId))]
    public AvailableOpponent AvailableOpponent = null!;

    [Required]
    public required string Token { get; set; }

    public BattleStatus BattleStatus { get; set; }
    public TxId? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
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

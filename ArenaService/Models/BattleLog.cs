using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("battle_logs")]
public class BattleLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string AttackerAvatarAddress { get; set; }

    // [ForeignKey(nameof(AttackerAvatarAddress), nameof(SeasonId))] - Use Fluent API
    public Participant Attacker { get; set; } = null!;

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string DefenderAvatarAddress { get; set; }

    // [ForeignKey(nameof(DefenderAvatarAddress), nameof(SeasonId))] - Use Fluent API
    public Participant Defender { get; set; } = null!;

    [Required]
    public required string Token { get; set; }

    public string? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public bool? IsVictory { get; set; }
    public int? ParticipantScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
    public long? BattleBlockIndex { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

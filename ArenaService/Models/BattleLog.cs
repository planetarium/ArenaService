using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("battle_logs")]
public class BattleLog
{
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [Required]
    public required string AttackerAvatarAddress { get; set; }
    public Participant Attacker { get; set; } = null!;

    public required string DefenderAvatarAddress { get; set; }
    public Participant Defender { get; set; } = null!;

    [Required]
    public required string Token { get; set; }

    public string? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public bool? IsVictory { get; set; }
    public int? ParticipantScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
    public long? BattleBlockIndex { get; set; }
}

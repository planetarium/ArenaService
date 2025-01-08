using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("battle_logs")]
public class BattleLog
{
    public int Id { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    public int OpponentId { get; set; }
    public Participant Opponent { get; set; } = null!;

    [Required]
    public required string Token { get; set; }

    public bool? IsVictory { get; set; }
    public int? ParticipantScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
    public long? BattleBlockIndex { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("battle_logs")]
public class BattleLog
{
    public int Id { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public required Participant Participant { get; set; }

    [Required]
    public int OpponentId { get; set; }
    public required Participant Opponent { get; set; }

    [Required]
    public int SeasonId { get; set; }
    public required Season Season { get; set; }

    public long BattleBlockIndex { get; set; }

    [Required]
    public bool IsVictory { get; set; }

    public int ParticipantScoreChange { get; set; }
    public int OpponentScoreChange { get; set; }
}

using ArenaService.Constants;
using ArenaService.Models;

namespace ArenaService.Dtos;

public class BattleLogResponse
{
    public int Id { get; set; }
    public int SeasonId { get; set; }

    public required string AttackerAvatarAddress { get; set; }

    public required string DefenderAvatarAddress { get; set; }

    public BattleStatus BattleStatus { get; set; }
    public string? TxId { get; set; }
    public string? TxStatus { get; set; }
    public bool? IsVictory { get; set; }
    public int? ParticipantScore { get; set; }
    public int? ParticipantScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
    public long? BattleBlockIndex { get; set; }
}

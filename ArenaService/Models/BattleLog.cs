namespace ArenaForge.Models;

public class BattleLog
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public required Participant Participant { get; set; }
    public int OpponentId { get; set; }
    public required Participant Opponent { get; set; }
    public int SeasonId { get; set; }
    public required Season Season { get; set; }
    public DateTime BattleTime { get; set; }
    public bool IsVictory { get; set; }
    public int ParticipantScoreChange { get; set; }
    public int OpponentScoreChange { get; set; }
}

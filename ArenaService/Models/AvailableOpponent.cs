namespace ArenaService.Models;

public class AvailableOpponent
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public required Participant Participant { get; set; }
    public int OpponentId { get; set; }
    public required Participant Opponent { get; set; }
    public int SeasonId { get; set; }
    public required Season Season { get; set; }
    public DateTime RefillTime { get; set; }
}

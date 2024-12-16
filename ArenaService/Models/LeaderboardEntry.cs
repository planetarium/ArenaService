namespace ArenaService.Models;

public class LeaderboardEntry
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public required Participant Participant { get; set; }
    public int SeasonId { get; set; }
    public required Season Season { get; set; }
    public int Rank { get; set; }
    public int TotalScore { get; set; } = 1000;
}

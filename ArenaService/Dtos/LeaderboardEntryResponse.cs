namespace ArenaService.Dtos;

public class LeaderboardEntryResponse : ParticipantResponse
{
    public int Rank { get; set; }
    public required string ClanImageURL { get; set; }
}

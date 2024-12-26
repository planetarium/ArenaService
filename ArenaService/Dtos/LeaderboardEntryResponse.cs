namespace ArenaService.Dtos;

public class LeaderboardEntryResponse
{
    public required string AvatarAddress { get; set; }
    public required string NameWithHash { get; set; }
    public int PortraitId { get; set; }
    public int Rank { get; set; }
    public int TotalScore { get; set; }
}

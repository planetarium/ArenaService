namespace ArenaService.Models;

public class Participant
{
    public int Id { get; set; }
    public required string AvatarAddress { get; set; }
    public required string Nickname { get; set; }
    public required ICollection<BattleLog> BattleLogs { get; set; }
    public required ICollection<LeaderboardEntry> LeaderboardEntries { get; set; }
}

namespace ArenaForge.Models;

public class Season
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TicketRefillInterval { get; set; }
    public required ICollection<BattleLog> BattleLogs { get; set; }
    public required ICollection<LeaderboardEntry> LeaderboardEntries { get; set; }
}

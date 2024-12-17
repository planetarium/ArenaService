using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("seasons")]
public class Season
{
    public int Id { get; set; }

    [Required]
    public long StartBlockIndex { get; set; }

    [Required]
    public long EndBlockIndex { get; set; }

    public int TicketRefillInterval { get; set; } = 600;

    public required ICollection<Participant> Participants { get; set; }
    public required ICollection<BattleLog> BattleLogs { get; set; }
    public required ICollection<LeaderboardEntry> Leaderboard { get; set; }
}

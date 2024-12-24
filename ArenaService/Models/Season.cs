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

    public bool IsActivated { get; set; } = false;

    public ICollection<Participant> Participants { get; set; } = null!;
    public ICollection<BattleLog> BattleLogs { get; set; } = null!;
    public ICollection<LeaderboardEntry> Leaderboard { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("leaderboard")]
public class LeaderboardEntry
{
    public int Id { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public Participant Participant { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;

    [Required]
    public int Rank { get; set; }
    public int TotalScore { get; set; } = 1000;
}

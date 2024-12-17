using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("leaderboard")]
public class LeaderboardEntry
{
    public int Id { get; set; }

    [Required]
    public int ParticipantId { get; set; }
    public required Participant Participant { get; set; }

    [Required]
    public int SeasonId { get; set; }
    public required Season Season { get; set; }

    [Required]
    public int Rank { get; set; }
    public int TotalScore { get; set; } = 1000;
}

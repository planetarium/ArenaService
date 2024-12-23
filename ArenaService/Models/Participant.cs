using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("participants")]
[Index(nameof(AvatarAddress))]
public class Participant
{
    public int Id { get; set; }

    [Required]
    public required string AvatarAddress { get; set; }

    [Required]
    public required string NameWithHash { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [Required]
    public int PortraitId { get; set; }

    public Season Season { get; set; } = null!;
    public ICollection<BattleLog> BattleLogs { get; set; } = null!;
    public ICollection<LeaderboardEntry> Leaderboard { get; set; } = null!;
}

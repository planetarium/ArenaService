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

    public int Cp { get; set; } = 0;

    public int PortraitId { get; set; }

    public required Season Season { get; set; }
    public required ICollection<BattleLog> BattleLogs { get; set; }
    public required ICollection<LeaderboardEntry> Leaderboard { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("participants")]
[PrimaryKey(nameof(AvatarAddress), nameof(SeasonId))]
public class Participant
{
    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    [ForeignKey(nameof(AvatarAddress))]
    public User User { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    public int InitializedScore { get; set; } = 1000;
    public int Score { get; set; } = 1000;
    public int TotalWin { get; set; } = 0;
    public int TotalLose { get; set; } = 0;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AvailableOpponent> AvailableOpponents { get; set; } = null!;
}

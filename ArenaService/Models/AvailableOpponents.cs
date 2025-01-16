using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

[Table("available_opponents")]
public class AvailableOpponents
{
    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string AvatarAddress { get; set; }

    [Required]
    public int RoundId { get; set; }
    public Round Round { get; set; } = null!;

    [Required]
    [Column("opponent_avatar_addresses", TypeName = "text[]")]
    public List<string> OpponentAvatarAddresses { get; set; } = new List<string>();

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArenaService.Models;

public enum UpdateSource
{
    FREE,
    UserPaid
}

[Table("available_opponents")]
public class AvailableOpponent
{
    [Required]
    public int SeasonId { get; set; }

    [Required]
    public required string ParticipantAvatarAddress { get; set; }
    public Participant Participant { get; set; } = null!;

    [Required]
    public int IntervalId { get; set; }
    public ArenaInterval ArenaInterval { get; set; } = null!;

    [Required]
    [Column("opponent_avatar_addresses", TypeName = "text[]")]
    public List<string> OpponentAvatarAddresses { get; set; } = new List<string>();

    public UpdateSource UpdateSource { get; set; }

    public string CostPaid { get; set; }
}

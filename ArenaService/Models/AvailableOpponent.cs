using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models;

[Table("available_opponents")]
public class AvailableOpponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string AvatarAddress { get; set; }

    // [ForeignKey(nameof(AvatarAddress), nameof(SeasonId))] - Use Fluent API
    public Participant MyParticipant { get; set; } = null!;

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    public int RoundId { get; set; }

    [ForeignKey(nameof(RoundId))]
    public Round Round { get; set; } = null!;

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string OpponentAvatarAddress { get; set; }

    // [ForeignKey(nameof(OpponentAvatarAddress), nameof(SeasonId))] - Use Fluent API
    public Participant Opponent { get; set; } = null!;

    [Required]
    public int RefreshRequestId { get; set; }

    [ForeignKey(nameof(RefreshRequestId))]
    public RefreshRequest RefreshRequest { get; set; } = null!;

    [Required]
    public int GroupId { get; set; }

    public int? BattleLogId { get; set; } = null;

    [ForeignKey(nameof(BattleLogId))]
    public BattleLog BattleLog { get; set; } = null!;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

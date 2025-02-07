using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Libplanet.Crypto;

namespace ArenaService.Shared.Models;

[Table("available_opponents")]
public class AvailableOpponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    public Participant Me { get; set; } = null!;

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
    public Address OpponentAvatarAddress { get; set; }

    public Participant Opponent { get; set; } = null!;

    [Required]
    public int GroupId { get; set; }

    public int? SuccessBattleId { get; set; } = null;

    [ForeignKey(nameof(SuccessBattleId))]
    public Battle SuccessBattle { get; set; } = null!;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "timestamptz")]
    public DateTime? DeletedAt { get; set; } = null;

    public ICollection<Battle> Battles { get; set; } = null!;
}

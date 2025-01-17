using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("refresh_requests")]
public class RefreshRequest
{
    [Key]
    public int Id { get; set; }

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
    public required string AvatarAddress { get; set; }

    [Required]
    public int RefreshPriceDetailId { get; set; }

    [ForeignKey(nameof(RefreshPriceDetailId))]
    public RefreshPriceDetail RefreshPriceDetail { get; set; } = null!;

    public string? TxId { get; set; }

    public TxStatus? TxStatus { get; set; }

    [Required]
    [Column("specified_avatar_addresses", TypeName = "text[]")]
    public List<string> SpecifiedOpponentAvatarAddresses { get; set; } = new List<string>();

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<AvailableOpponentsRefreshRequest> AvailableOpponentsRefreshRequests { get; set; } =
        new();
}

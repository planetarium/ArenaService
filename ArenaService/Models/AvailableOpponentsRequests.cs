using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Constants;

namespace ArenaService.Models;

[Table("available_opponents_requests")]
public class AvailableOpponentsRequest
{
    public int Id { get; set; }

    [Required]
    public int RoundId { get; set; }
    public Round Round { get; set; } = null!;

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public required string AvatarAddress { get; set; }

    [Required]
    public UpdateSource UpdateSource { get; set; }

    [Required]
    public int CostPaid { get; set; } = 0;

    public string? TxId { get; set; }

    public TxStatus? TxStatus { get; set; }

    [Required]
    [Column("requested_avatar_addresses", TypeName = "text[]")]
    public List<string> RequestedOpponentAddresses { get; set; } = new List<string>();

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamp")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

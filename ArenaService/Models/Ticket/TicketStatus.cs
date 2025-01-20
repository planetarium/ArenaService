using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models.Ticket;

[Table("ticket_statuses")]
[Index(
    nameof(SeasonId),
    nameof(RoundId),
    nameof(AvatarAddress),
    nameof(TicketType),
    IsUnique = true
)]
public class TicketStatus
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

    public required Participant Participant { get; set; } = null!;

    [Required]
    public TicketType TicketType { get; set; }

    [Required]
    public int RemainingCount { get; set; }

    [Required]
    public int UsedCount { get; set; } = 0;

    [Required]
    public int PurchaseCount { get; set; } = 0;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

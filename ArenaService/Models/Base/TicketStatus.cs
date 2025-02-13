using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Models.Ticket;

public abstract class TicketStatus
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SeasonId { get; set; }

    [ForeignKey(nameof(SeasonId))]
    public Season Season { get; set; } = null!;

    [Required]
    [StringLength(40, MinimumLength = 40)]
    public Address AvatarAddress { get; set; }

    public Participant Participant { get; set; } = null!;

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

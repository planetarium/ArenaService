using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;

namespace ArenaService.Models.Ticket;

[Table("ticket_usage_logs")]
public class TicketUsageLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TicketStatusId { get; set; }

    [ForeignKey(nameof(TicketStatusId))]
    public TicketStatus TicketStatus { get; set; } = null!;

    [Required]
    public TicketType TicketType { get; set; }

    public string? Memo { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

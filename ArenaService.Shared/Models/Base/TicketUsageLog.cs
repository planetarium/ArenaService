using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;

namespace ArenaService.Shared.Models.Ticket;

public abstract class TicketUsageLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "timestamptz")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

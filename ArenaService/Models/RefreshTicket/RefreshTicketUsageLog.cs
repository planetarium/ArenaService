using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.RefreshTicket;

[Table("refresh_ticket_usage_logs")]
public class RefreshTicketUsageLog : TicketUsageLog
{
    [Required]
    public int RefreshTicketStatusPerRoundId { get; set; }

    [ForeignKey(nameof(RefreshTicketStatusPerRoundId))]
    public RefreshTicketStatusPerRound RefreshTicketStatusPerRound { get; set; } = null!;

    [Required]
    public int AvailableOpponentId { get; set; }
}

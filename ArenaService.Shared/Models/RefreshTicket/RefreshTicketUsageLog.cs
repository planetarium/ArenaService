using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;
using Libplanet.Crypto;

namespace ArenaService.Shared.Models.RefreshTicket;

[Table("refresh_ticket_usage_logs")]
public class RefreshTicketUsageLog : TicketUsageLog
{
    [Required]
    public int RefreshTicketStatusPerRoundId { get; set; }

    [ForeignKey(nameof(RefreshTicketStatusPerRoundId))]
    public RefreshTicketStatusPerRound RefreshTicketStatusPerRound { get; set; } = null!;

    [Column(TypeName = "integer[]")]
    public required List<int> SpecifiedOpponentIds { get; set; }
}

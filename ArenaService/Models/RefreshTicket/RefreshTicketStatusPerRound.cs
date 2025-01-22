using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.RefreshTicket;

[Table("refresh_ticket_statuses_per_round")]
public class RefreshTicketStatusPerRound : TicketStatus
{
    [Required]
    public int RoundId { get; set; }

    [ForeignKey(nameof(RoundId))]
    public Round Round { get; set; } = null!;

    [Required]
    public int RefreshTicketPolicyId { get; set; }

    [ForeignKey(nameof(RefreshTicketPolicyId))]
    public RefreshTicketPolicy RefreshTicketPolicy { get; set; } = null!;

    [Required]
    public int RemainingCount { get; set; }
}

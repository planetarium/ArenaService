using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;
using Microsoft.EntityFrameworkCore;

namespace ArenaService.Shared.Models.RefreshTicket;

[Table("refresh_ticket_statuses_per_round")]
[Index(nameof(AvatarAddress), nameof(SeasonId), nameof(RoundId), IsUnique = true)]
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

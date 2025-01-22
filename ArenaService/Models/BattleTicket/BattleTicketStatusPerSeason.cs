using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.BattleTicket;

[Table("battle_ticket_statuses_per_season")]
public class BattleTicketStatusPerSeason : TicketStatus
{
    [Required]
    public int BattleTicketPolicyId { get; set; }

    [ForeignKey(nameof(BattleTicketPolicyId))]
    public BattleTicketPolicy BattleTicketPolicy { get; set; } = null!;
}

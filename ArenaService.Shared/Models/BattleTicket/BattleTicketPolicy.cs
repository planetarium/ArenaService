using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;

namespace ArenaService.Shared.Models.BattleTicket;

[Table("battle_ticket_policies")]
public class BattleTicketPolicy : TicketPolicy
{
    [Required]
    public int MaxPurchasableTicketsPerSeason { get; set; }
}

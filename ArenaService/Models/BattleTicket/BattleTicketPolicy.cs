using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.BattleTicket;

[Table("battle_ticket_policies")]
public class BattleTicketPolicy : TicketPolicy
{
    [Required]
    public int MaxPurchasableTicketsPerSeason { get; set; }
}

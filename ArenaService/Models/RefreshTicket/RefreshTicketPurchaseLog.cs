using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;
using Libplanet.Crypto;

namespace ArenaService.Models.RefreshTicket;

[Table("refresh_ticket_purchase_logs")]
public class RefreshTicketPurchaseLog : TicketPurchaseLog
{
}

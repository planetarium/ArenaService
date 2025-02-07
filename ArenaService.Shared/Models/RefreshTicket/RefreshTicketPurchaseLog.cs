using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;
using Libplanet.Crypto;

namespace ArenaService.Shared.Models.RefreshTicket;

[Table("refresh_ticket_purchase_logs")]
public class RefreshTicketPurchaseLog : TicketPurchaseLog
{
}

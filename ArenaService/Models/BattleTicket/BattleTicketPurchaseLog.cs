using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;
using Libplanet.Crypto;

namespace ArenaService.Models.BattleTicket;

[Table("battle_ticket_purchase_logs")]
public class BattleTicketPurchaseLog : TicketPurchaseLog { }

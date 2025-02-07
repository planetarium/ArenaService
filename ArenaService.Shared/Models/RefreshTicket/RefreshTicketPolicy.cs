using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.Ticket;

namespace ArenaService.Shared.Models.RefreshTicket;

[Table("refresh_ticket_policies")]
public class RefreshTicketPolicy : TicketPolicy { }

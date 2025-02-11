using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArenaService.Models.Enums;
using ArenaService.Models.Ticket;

namespace ArenaService.Models.RefreshTicket;

[Table("refresh_ticket_policies")]
public class RefreshTicketPolicy : TicketPolicy { }

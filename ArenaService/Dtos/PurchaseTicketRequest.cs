using ArenaService.Models.Enums;

namespace ArenaService.Dtos;

public class PurchaseTicketRequest
{
    public TicketType TicketType { get; set; }
    public int TicketCount { get; set; }
}

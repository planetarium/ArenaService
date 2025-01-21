using ArenaService.Models.Enums;

namespace ArenaService.Dtos;

public class PurchaseTicketRequest
{
    public int TicketCount { get; set; }
    public string TxId { get; set; }
}

using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class PurchaseTicketRequest
{
    public int TicketCount { get; set; }
    public TxId TxId { get; set; }
}

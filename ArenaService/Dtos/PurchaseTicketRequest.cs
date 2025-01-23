using ArenaService.Models.Enums;
using Libplanet.Types.Tx;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class PurchaseTicketRequest
{
    public int TicketCount { get; set; }
    public decimal PurchasePrice { get; set; }
    public TxId TxId { get; set; }
}

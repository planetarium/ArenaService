using ArenaService.Models.Enums;
using Libplanet.Types.Tx;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class PurchaseTicketRequest
{
    public required int TicketCount { get; set; }
    public required decimal PurchasePrice { get; set; }
    public required TxId TxId { get; set; }
}

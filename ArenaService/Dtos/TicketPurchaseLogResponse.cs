using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class TicketPurchaseLogResponse
{
    public int SeasonId { get; set; }
    public int RoundId { get; set; }
    public decimal? AmountPaid { get; set; }
    public int PurchaseCount { get; set; }
    public PurchaseStatus PurchaseStatus { get; set; }
    public TxId TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
}

using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class TicketPurchaseLogResponse
{
    public required int SeasonId { get; set; }
    public required int RoundId { get; set; }
    public decimal? AmountPaid { get; set; }
    public required int PurchaseCount { get; set; }
    public required PurchaseStatus PurchaseStatus { get; set; }
    public required TxId TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
}

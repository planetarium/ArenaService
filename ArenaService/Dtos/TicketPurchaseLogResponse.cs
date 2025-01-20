using ArenaService.Models.Enums;

namespace ArenaService.Dtos;


public class TicketPurchaseLogResponse
{
    public TicketType TicketType { get; set; }
    public int PurchaseOrderPerRound { get; set; }
    public decimal PurchasePrice { get; set; }
    public int PurchaseCount { get; set; }
    public PurchaseStatus PurchaseStatus { get; set; }
    public required string TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
}

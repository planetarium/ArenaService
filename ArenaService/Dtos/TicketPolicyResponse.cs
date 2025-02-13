namespace ArenaService.Dtos;

public class TicketPolicyResponse
{
    public required int DefaultTicketsPerRound { get; set; }
    public required int MaxPurchasableTicketsPerRound { get; set; }
    public required List<decimal> PurchasePrices { get; set; }
}

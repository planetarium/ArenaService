namespace ArenaService.Dtos;

public class TicketPolicyResponse
{
    public int DefaultTicketsPerRound { get; set; }
    public int MaxPurchasableTicketsPerRound { get; set; }
    public List<decimal> PurchasePrices { get; set; }
}

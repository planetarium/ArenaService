namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models.Ticket;

public static class TicketPolicyExtensions
{
    public static TicketPolicyResponse ToResponse(this TicketPolicy policy)
    {
        return new TicketPolicyResponse
        {
            DefaultTicketsPerRound = policy.DefaultTicketsPerRound,
            MaxPurchasableTicketsPerRound = policy.MaxPurchasableTicketsPerRound,
            PurchasePrices = policy.PurchasePrices,
        };
    }
}

namespace ArenaService.Shared.Dtos;

using ArenaService.Shared.Models;
using ArenaService.Shared.Models.RefreshTicket;

public class RefreshTicketStatusResponse : TicketStatusResponse
{
    public static RefreshTicketStatusResponse FromRefreshStatusModel(
        RefreshTicketStatusPerRound roundStatus
    )
    {
        return new RefreshTicketStatusResponse
        {
            TicketsPurchasedPerRound = roundStatus.PurchaseCount,
            TicketsUsedPerRound = roundStatus.UsedCount,
            RemainingTicketsPerRound = roundStatus.RemainingCount,
            RemainingPurchasableTicketsPerRound =
                roundStatus.RefreshTicketPolicy.MaxPurchasableTicketsPerRound
                - roundStatus.PurchaseCount,
            IsUnused = roundStatus.UsedCount == 0,
            NextNCGCosts = roundStatus
                .RefreshTicketPolicy.PurchasePrices.Skip(roundStatus.PurchaseCount)
                .ToList(),
        };
    }

    public static RefreshTicketStatusResponse CreateRefreshTicketDefault(Season season)
    {
        return new RefreshTicketStatusResponse
        {
            TicketsPurchasedPerRound = 0,
            TicketsUsedPerRound = 0,
            RemainingTicketsPerRound = season.RefreshTicketPolicy.DefaultTicketsPerRound,
            RemainingPurchasableTicketsPerRound = season
                .RefreshTicketPolicy
                .MaxPurchasableTicketsPerRound,
            IsUnused = true,
            NextNCGCosts = season.RefreshTicketPolicy.PurchasePrices,
        };
    }
}

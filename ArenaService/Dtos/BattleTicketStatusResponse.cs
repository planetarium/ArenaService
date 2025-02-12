namespace ArenaService.Dtos;

using ArenaService.Models;
using ArenaService.Models.BattleTicket;
using Swashbuckle.AspNetCore.Annotations;

public class BattleTicketStatusResponse : TicketStatusResponse
{
    [SwaggerSchema("현재 시즌에서 구매한 티켓의 개수")]
    public required int TicketsPurchasedPerSeason { get; set; }

    [SwaggerSchema("현재 시즌에서 사용한 티켓의 개수")]
    public required int TicketsUsedPerSeason { get; set; }

    [SwaggerSchema("현재 시즌에서 구매 가능한 티켓의 개수")]
    public required int RemainingPurchasableTicketsPerSeason { get; set; }

    public static BattleTicketStatusResponse FromBattleStatusModels(
        BattleTicketStatusPerSeason seasonStatus,
        BattleTicketStatusPerRound roundStatus
    )
    {
        return new BattleTicketStatusResponse
        {
            TicketsPurchasedPerSeason = seasonStatus.PurchaseCount,
            TicketsUsedPerSeason = seasonStatus.UsedCount,
            RemainingPurchasableTicketsPerSeason =
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                - seasonStatus.PurchaseCount,
            TicketsPurchasedPerRound = roundStatus.PurchaseCount,
            TicketsUsedPerRound = roundStatus.UsedCount,
            RemainingTicketsPerRound = roundStatus.RemainingCount,
            RemainingPurchasableTicketsPerRound =
                roundStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                    - roundStatus.PurchaseCount
                <= 0
                    ? 0
                    : seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                        - seasonStatus.PurchaseCount
                    > roundStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                        ? roundStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                            - roundStatus.PurchaseCount
                        : seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                            - seasonStatus.PurchaseCount,
            IsUnused = roundStatus.UsedCount == 0,
            NextNCGCosts = seasonStatus
                .BattleTicketPolicy.PurchasePrices.Skip(seasonStatus.PurchaseCount)
                .ToList(),
        };
    }

    public static BattleTicketStatusResponse CreateBattleTicketDefault(Season season)
    {
        return new BattleTicketStatusResponse
        {
            TicketsPurchasedPerSeason = 0,
            TicketsUsedPerSeason = 0,
            RemainingPurchasableTicketsPerSeason = season
                .BattleTicketPolicy
                .MaxPurchasableTicketsPerSeason,
            TicketsPurchasedPerRound = 0,
            TicketsUsedPerRound = 0,
            RemainingTicketsPerRound = season.BattleTicketPolicy.DefaultTicketsPerRound,
            RemainingPurchasableTicketsPerRound = season
                .BattleTicketPolicy
                .MaxPurchasableTicketsPerRound,
            IsUnused = true,
            NextNCGCosts = season.BattleTicketPolicy.PurchasePrices,
        };
    }

    public static BattleTicketStatusResponse CreateBattleTicketDefault(
        Season season,
        BattleTicketStatusPerSeason seasonStatus
    )
    {
        return new BattleTicketStatusResponse
        {
            TicketsPurchasedPerSeason = seasonStatus.PurchaseCount,
            TicketsUsedPerSeason = seasonStatus.UsedCount,
            RemainingPurchasableTicketsPerSeason =
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                - seasonStatus.PurchaseCount,
            TicketsPurchasedPerRound = 0,
            TicketsUsedPerRound = 0,
            RemainingTicketsPerRound = season.BattleTicketPolicy.DefaultTicketsPerRound,
            RemainingPurchasableTicketsPerRound =
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                    - seasonStatus.PurchaseCount
                > seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                    ? seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                    : seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
                        - seasonStatus.PurchaseCount,
            IsUnused = true,
            NextNCGCosts = seasonStatus
                .BattleTicketPolicy.PurchasePrices.Skip(seasonStatus.PurchaseCount)
                .ToList(),
        };
    }
}

namespace ArenaService.Shared.Dtos;

using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using Swashbuckle.AspNetCore.Annotations;

public class BattleTicketStatusResponse : TicketStatusResponse
{
    [SwaggerSchema("현재 시즌에서 구매한 티켓의 개수")]
    public required int TicketsPurchasedPerSeason { get; set; }

    [SwaggerSchema("현재 시즌에서 사용한 티켓의 개수")]
    public required int TicketsUsedPerSeason { get; set; }

    [SwaggerSchema("현재 시즌에서 구매 가능한 티켓의 개수")]
    public required int RemainingPurchasableTicketsPerSeason { get; set; }

    private static int CalculateRemainingPurchasableTicketsPerRound(
        int roundPurchaseCount,
        int seasonPurchaseCount,
        int maxPurchasableTicketsPerRound,
        int maxPurchasableTicketsPerSeason
    )
    {
        // 라운드에서 더 이상 구매할 수 없는 경우
        if (maxPurchasableTicketsPerRound - roundPurchaseCount <= 0)
            return 0;

        // 시즌에서 더 이상 구매할 수 없는 경우
        if (maxPurchasableTicketsPerSeason - seasonPurchaseCount <= 0)
            return 0;

        // 시즌 제한과 라운드 제한 중 더 작은 값을 반환
        var remainingSeasonTickets = maxPurchasableTicketsPerSeason - seasonPurchaseCount;
        var remainingRoundTickets = maxPurchasableTicketsPerRound - roundPurchaseCount;

        return remainingSeasonTickets < remainingRoundTickets
            ? remainingSeasonTickets
            : remainingRoundTickets;
    }

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
            RemainingPurchasableTicketsPerRound = CalculateRemainingPurchasableTicketsPerRound(
                roundStatus.PurchaseCount,
                seasonStatus.PurchaseCount,
                roundStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound,
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
            ),
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
            RemainingPurchasableTicketsPerRound = CalculateRemainingPurchasableTicketsPerRound(
                0,
                seasonStatus.PurchaseCount,
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound,
                seasonStatus.BattleTicketPolicy.MaxPurchasableTicketsPerSeason
            ),
            IsUnused = true,
            NextNCGCosts = seasonStatus
                .BattleTicketPolicy.PurchasePrices.Skip(seasonStatus.PurchaseCount)
                .ToList(),
        };
    }
}

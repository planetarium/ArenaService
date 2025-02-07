namespace ArenaService.Dtos;

using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using Swashbuckle.AspNetCore.Annotations;

public class TicketStatusResponse
{
    [SwaggerSchema("현재 라운드에서 구매한 티켓의 개수")]
    public required int TicketsPurchasedPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 사용한 티켓의 개수")]
    public required int TicketsUsedPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 사용 가능한(남아있는) 티켓의 개수")]
    public required int RemainingTicketsPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 구매 가능한 티켓의 개수")]
    public required int RemainingPurchasableTicketsPerRound { get; set; }

    [SwaggerSchema("아직 티켓을 사용한 적이 없음")]
    public required bool IsUnused { get; set; }

    [SwaggerSchema("다음에 지불해야할 NCG")]
    public required List<decimal> NextNCGCosts { get; set; }

    public static TicketStatusResponse FromBattleStatusModels(
        BattleTicketStatusPerSeason seasonStatus,
        BattleTicketStatusPerRound roundStatus
    )
    {
        return new TicketStatusResponse
        {
            TicketsPurchasedPerRound = roundStatus.PurchaseCount,
            TicketsUsedPerRound = roundStatus.UsedCount,
            RemainingTicketsPerRound = roundStatus.RemainingCount,
            RemainingPurchasableTicketsPerRound =
                roundStatus.BattleTicketPolicy.MaxPurchasableTicketsPerRound
                - roundStatus.PurchaseCount,
            IsUnused = roundStatus.UsedCount == 0,
            NextNCGCosts = seasonStatus
                .BattleTicketPolicy.PurchasePrices.Skip(seasonStatus.PurchaseCount)
                .ToList(),
        };
    }

    public static TicketStatusResponse FromRefreshStatusModel(
        RefreshTicketStatusPerRound roundStatus
    )
    {
        return new TicketStatusResponse
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

    public static TicketStatusResponse CreateBattleTicketDefault(Season season)
    {
        return new TicketStatusResponse
        {
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

    public static TicketStatusResponse CreateBattleTicketDefault(
        Season season,
        BattleTicketStatusPerSeason seasonStatus
    )
    {
        return new TicketStatusResponse
        {
            TicketsPurchasedPerRound = 0,
            TicketsUsedPerRound = 0,
            RemainingTicketsPerRound = season.BattleTicketPolicy.DefaultTicketsPerRound,
            RemainingPurchasableTicketsPerRound = season
                .BattleTicketPolicy
                .MaxPurchasableTicketsPerRound,
            IsUnused = true,
            NextNCGCosts = seasonStatus
                .BattleTicketPolicy.PurchasePrices.Skip(seasonStatus.PurchaseCount)
                .ToList(),
        };
    }

    public static TicketStatusResponse CreateRefreshTicketDefault(Season season)
    {
        return new TicketStatusResponse
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

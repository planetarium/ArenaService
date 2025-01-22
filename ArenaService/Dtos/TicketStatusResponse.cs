namespace ArenaService.Dtos;

using ArenaService.Models;
using ArenaService.Models.BattleTicket;
using ArenaService.Models.RefreshTicket;
using Swashbuckle.AspNetCore.Annotations;

public class TicketStatusResponse
{
    [SwaggerSchema("현재 라운드에서 구매한 티켓의 개수")]
    public int TicketsPurchasedPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 사용한 티켓의 개수")]
    public int TicketsUsedPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 사용 가능한(남아있는) 티켓의 개수")]
    public int RemainingTicketsPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 구매 가능한 티켓의 개수")]
    public int RemainingPurchasableTicketsPerRound { get; set; }

    [SwaggerSchema("아직 티켓을 사용한 적이 없음")]
    public bool IsUnused { get; set; }

    [SwaggerSchema("다음에 지불해야할 NCG")]
    public decimal NextNCGCost { get; set; }

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
            NextNCGCost = seasonStatus.BattleTicketPolicy.GetPrice(seasonStatus.PurchaseCount),
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
            NextNCGCost = roundStatus.RefreshTicketPolicy.GetPrice(roundStatus.PurchaseCount),
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
            NextNCGCost = season.BattleTicketPolicy.GetPrice(0),
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
            NextNCGCost = season.RefreshTicketPolicy.GetPrice(0),
        };
    }
}

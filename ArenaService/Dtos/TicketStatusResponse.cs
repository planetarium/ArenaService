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
}

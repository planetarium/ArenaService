namespace ArenaService.Dtos;

using Swashbuckle.AspNetCore.Annotations;

public class TicketStatusResponse
{
    [SwaggerSchema("현재까지 구매한 총 티켓의 개수")]
    public int TotalPurchasedTickets { get; set; }

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
    public int NextRefreshNCGCost { get; set; }

}

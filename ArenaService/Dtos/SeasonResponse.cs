using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class SeasonResponse
{
    public int Id { get; set; }
    public ArenaType ArenaType { get; set; }
    public long StartBlockIndex { get; set; }
    public long EndBlockIndex { get; set; }
    public int Interval { get; set; }
    public long RequiredMedalCount { get; set; }

    [SwaggerSchema("현재 시즌에서 티켓을 구매할 때 지불해야할 NCG Cost 리스트")]
    public float[] TicketPrices { get; set; }

    [SwaggerSchema("현재 시즌에서 라운드 당 총 구매 가능한 티켓의 개수")]
    public int MaxPurchasableTicketsPerRound { get; set; }

    [SwaggerSchema("현재 시즌에서 라운드 당 무료로 사용 가능한 티켓의 개수")]
    public int MaxFreeTicketsPerRound { get; set; }

    [SwaggerSchema("현재 시즌에서 라운드 당 사용 가능한(무료, 유료 포함) 총 티켓의 개수")]
    public int MaxTotalTicketsPerRound { get; set; }

    [SwaggerSchema("현재 시즌에서 리프레시를 구매할 때 지불해야할 NCG Cost 리스트")]
    public float[] RefreshPricesPerRound { get; set; }

    [SwaggerSchema("현재 시즌에서 라운드 당 구매 가능한 리프레시 개수")]
    public int MaxPurchasableRefreshesPerRound { get; set; }

    [SwaggerSchema("현재 시즌에서 라운드 당 무료로 사용 가능한 리프레시 개수")]
    public int MaxFreeRefreshesPerRound { get; set; }

    public int TotalPrize { get; set; }
    public int PrizeDetailSiteURL { get; set; }

    public List<RoundResponse> Rounds { get; set; } = new List<RoundResponse>();

    public SeasonResponse() { }
}

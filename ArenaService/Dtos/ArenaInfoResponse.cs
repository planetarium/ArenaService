namespace ArenaService.Dtos;

using Swashbuckle.AspNetCore.Annotations;

public class ArenaInfoResponse
{
    public required string AvatarAddress { get; set; }

    public required string NameWithHash { get; set; }

    public int PortraitId { get; set; }
    public long Cp { get; set; }
    public int Level { get; set; }
    public int SeasonId { get; set; }

    public ClanResponse ClanInfo { get; set; }

    public int Score { get; set; }
    public int CurrentRoundScoreChange { get; set; }

    public int Rank { get; set; }
    public int CurrentRoundRankChange { get; set; }
    public int TotalWin { get; set; }
    public int TotalLose { get; set; }
    public int CurrentRoundWinChange { get; set; }
    public int CurrentRoundLoseChange { get; set; }

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

    [SwaggerSchema("현재 라운드에서 구매한 리프레시 개수")]
    public int PurchasedRefreshesPerRound { get; set; }

    [SwaggerSchema("다음에 리프레시에 지불해야할 NCG")]
    public int NextRefreshNCGCost { get; set; }

    [SwaggerSchema("현재 라운드에서 사용한 리프레시 개수")]
    public int RefreshesUsedPerRound { get; set; }

    [SwaggerSchema("현재 라운드에서 남아있는 리프레시 개수")]
    public int RemainingRefreshesPerRound { get; set; }
}

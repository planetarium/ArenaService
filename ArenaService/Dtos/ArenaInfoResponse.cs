namespace ArenaService.Dtos;

public class ArenaInfoResponse
{
    public required UserResponse User { get; set; }

    public ClanResponse? ClanInfo { get; set; }

    public required int SeasonId { get; set; }
    public required int RoundId { get; set; }
    public required int Score { get; set; }
    public required int Rank { get; set; }
    public required int CurrentRoundScoreChange { get; set; }
    public required int CurrentRoundRankChange { get; set; }
    public required int TotalWin { get; set; }
    public required int TotalLose { get; set; }
    public required int CurrentRoundWinChange { get; set; }
    public required int CurrentRoundLoseChange { get; set; }
    public required TicketStatusResponse BattleTicketStatus { get; set; }
    public required TicketStatusResponse RefreshTicketStatus { get; set; }
}

namespace ArenaService.Dtos;

public class ArenaInfoResponse
{
    public UserResponse User { get; set; }

    public ClanResponse ClanInfo { get; set; }

    public int SeasonId { get; set; }
    public int RoundId { get; set; }
    public int Score { get; set; }
    public int Rank { get; set; }
    public int CurrentRoundScoreChange { get; set; }
    public int CurrentRoundRankChange { get; set; }
    public int TotalWin { get; set; }
    public int TotalLose { get; set; }
    public int CurrentRoundWinChange { get; set; }
    public int CurrentRoundLoseChange { get; set; }
    public TicketStatusResponse BattleTicketStatus { get; set; }
    public TicketStatusResponse RefreshTicketStatus { get; set; }
}

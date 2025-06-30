using ArenaService.Shared.Constants;

namespace ArenaService.Shared.Dtos;

public class CompletedSeasonLeaderboardResponse
{
    public SimpleSeasonResponse Season { get; set; } = new();
    public List<LeaderboardEntryResponse> Leaderboard { get; set; } = new();
}

public class SimpleSeasonResponse
{
    public int Id { get; set; }
    public int SeasonGroupId { get; set; }
    public long StartBlock { get; set; }
    public long EndBlock { get; set; }
    public ArenaType ArenaType { get; set; }
}

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public string AvatarAddress { get; set; } = string.Empty;
    public string AgentAddress { get; set; } = string.Empty;
    public string NameWithHash { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalWin { get; set; }
    public int TotalLose { get; set; }
    public int Level { get; set; }
} 
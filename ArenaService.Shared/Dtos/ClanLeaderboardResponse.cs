using ArenaService.Shared.Constants;

namespace ArenaService.Shared.Dtos;

public class ClanLeaderboardResponse
{
    public required List<ClanResponse> Leaderboard { get; set; }
    public ClanResponse? MyClan { get; set; }
}

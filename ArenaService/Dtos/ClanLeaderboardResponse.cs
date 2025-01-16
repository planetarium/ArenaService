using ArenaService.Constants;

namespace ArenaService.Dtos;

public class ClanLeaderboardResponse
{
    public List<ClanResponse> leaderboard { get; set; }
    public ClanResponse myClan { get; set; }
}

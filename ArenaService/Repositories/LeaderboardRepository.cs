namespace ArenaService.Repositories;

using ArenaService.Data;
using ArenaService.Models;
using Microsoft.EntityFrameworkCore;

public interface ILeaderboardRepository
{
    // Task<List<LeaderboardEntry>> GetLeaderboard(int seasonId, int offset, int limit);
    Task<LeaderboardEntry?> GetMyRankAsync(int seasonId, int participantId);
}

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly ArenaDbContext _context;

    public LeaderboardRepository(ArenaDbContext context)
    {
        _context = context;
    }

    // public async Task<Participant> GetLeaderboard(
    //     int seasonId,
    //     string avatarAddress,
    //     string nameWithHash,
    //     int portraitId
    // )
    // {
    //     var nextPage = context
    //         .Leaderboard.OrderBy(b => b.Date)
    //         .ThenBy(b => b.PostId)
    //         .Where(b => b.Date > lastDate || (b.Date == lastDate && b.PostId > lastId))
    //         .Take(10)
    //         .ToList();
    // }

    public async Task<LeaderboardEntry?> GetMyRankAsync(int seasonId, int participantId)
    {
        return await _context.Leaderboard.FirstOrDefaultAsync(lb =>
            lb.SeasonId == seasonId && lb.ParticipantId == participantId
        );
    }
}

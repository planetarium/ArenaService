using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IRankingRepository
{
    Task UpdateScoreAsync(string leaderboardKey, int participantId, int scoreChange);

    Task<int?> GetRankAsync(string leaderboardKey, int participantId);

    Task<int?> GetScoreAsync(string leaderboardKey, int participantId);

    Task<List<(int Rank, int ParticipantId, int Score)>> GetTopRankingsAsync(
        string leaderboardKey,
        int topN
    );

    Task<List<(int Rank, int ParticipantId, int Score)>> GetRankingsWithPaginationAsync(
        string leaderboardKey,
        int pageNumber,
        int pageSize
    );

    Task SyncLeaderboardAsync(string leaderboardKey, List<(int ParticipantId, int Score)> entries);
}

public class RankingRepository : IRankingRepository
{
    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateScoreAsync(string leaderboardKey, int participantId, int scoreChange)
    {
        await _redis.SortedSetIncrementAsync(
            leaderboardKey,
            $"participant:{participantId}",
            scoreChange
        );
    }

    public async Task<int?> GetRankAsync(string leaderboardKey, int participantId)
    {
        var rank = await _redis.SortedSetRankAsync(
            leaderboardKey,
            $"participant:{participantId}",
            Order.Descending
        );
        return rank.HasValue ? (int)rank.Value + 1 : null;
    }

    public async Task<int?> GetScoreAsync(string leaderboardKey, int participantId)
    {
        var score = await _redis.SortedSetScoreAsync(
            leaderboardKey,
            $"participant:{participantId}"
        );
        return score.HasValue ? (int)score.Value : null;
    }

    public async Task<List<(int Rank, int ParticipantId, int Score)>> GetTopRankingsAsync(
        string leaderboardKey,
        int topN
    )
    {
        var topRankings = await _redis.SortedSetRangeByRankWithScoresAsync(
            leaderboardKey,
            0,
            topN - 1,
            Order.Descending
        );

        return topRankings
            .Select(
                (entry, index) =>
                {
                    var participantId = int.Parse(entry.Element.ToString().Split(':')[1]);
                    return (Rank: index + 1, ParticipantId: participantId, Score: (int)entry.Score);
                }
            )
            .ToList();
    }

    public async Task<
        List<(int Rank, int ParticipantId, int Score)>
    > GetRankingsWithPaginationAsync(string leaderboardKey, int pageNumber, int pageSize)
    {
        int start = (pageNumber - 1) * pageSize;
        int end = start + pageSize - 1;

        var rankedParticipants = await _redis.SortedSetRangeByRankWithScoresAsync(
            leaderboardKey,
            start,
            end,
            Order.Descending
        );

        return rankedParticipants
            .Select(
                (entry, index) =>
                {
                    var participantId = int.Parse(entry.Element.ToString().Split(':')[1]);
                    return (
                        Rank: start + index + 1,
                        ParticipantId: participantId,
                        Score: (int)entry.Score
                    );
                }
            )
            .ToList();
    }

    public async Task SyncLeaderboardAsync(
        string leaderboardKey,
        List<(int ParticipantId, int Score)> entries
    )
    {
        foreach (var entry in entries)
        {
            await _redis.SortedSetAddAsync(
                leaderboardKey,
                $"participant:{entry.ParticipantId}",
                entry.Score
            );
        }
    }
}

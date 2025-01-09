using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IRankingRepository
{
    Task UpdateScoreAsync(
        string leaderboardKey,
        string participantAvatarAddress,
        int seasonId,
        int scoreChange
    );

    Task<int?> GetRankAsync(string leaderboardKey, string participantAvatarAddress, int seasonId);

    Task<int?> GetScoreAsync(string leaderboardKey, string participantAvatarAddress, int seasonId);

    Task<
        List<(int Rank, Address ParticipantAvatarAddress, int SeasonId, int Score)>
    > GetRankingsWithPaginationAsync(string rankingKey, int pageNumber, int pageSize);

    Task<
        List<(Address ParticipantAvatarAddress, int SeasonId, int Score)>
    > GetRandomParticipantsTempAsync(
        string rankingKey,
        string participantAvatarAddress,
        int seasonId,
        int score,
        int count
    );
}

public class RankingRepository : IRankingRepository
{
    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateScoreAsync(
        string leaderboardKey,
        string participantAvatarAddress,
        int seasonId,
        int scoreChange
    )
    {
        await _redis.SortedSetIncrementAsync(
            leaderboardKey,
            $"participant:{participantAvatarAddress}:{seasonId}",
            scoreChange
        );
    }

    public async Task<int?> GetRankAsync(
        string rankingKey,
        string participantAvatarAddress,
        int seasonId
    )
    {
        var rank = await _redis.SortedSetRankAsync(
            rankingKey,
            $"participant:{participantAvatarAddress}:{seasonId}",
            Order.Descending
        );
        return rank.HasValue ? (int)rank.Value + 1 : null;
    }

    public async Task<int?> GetScoreAsync(
        string rankingKey,
        string participantAvatarAddress,
        int seasonId
    )
    {
        var score = await _redis.SortedSetScoreAsync(
            rankingKey,
            $"participant:{participantAvatarAddress}:{seasonId}"
        );
        return score.HasValue ? (int)score.Value : null;
    }

    public async Task<
        List<(int Rank, Address ParticipantAvatarAddress, int SeasonId, int Score)>
    > GetRankingsWithPaginationAsync(string rankingKey, int pageNumber, int pageSize)
    {
        int start = (pageNumber - 1) * pageSize;
        int end = start + pageSize - 1;

        var rankedParticipants = await _redis.SortedSetRangeByRankWithScoresAsync(
            rankingKey,
            start,
            end,
            Order.Descending
        );

        return rankedParticipants
            .Select(
                (entry, index) =>
                {
                    var participantAvatarAddress = entry.Element.ToString().Split(':')[1];
                    var seasonId = int.Parse(entry.Element.ToString().Split(':')[2]);
                    return (
                        Rank: start + index + 1,
                        ParticipantAvatarAddress: new Address(participantAvatarAddress),
                        SeasonId: seasonId,
                        Score: (int)entry.Score
                    );
                }
            )
            .ToList();
    }

    public async Task<
        List<(Address ParticipantAvatarAddress, int SeasonId, int Score)>
    > GetRandomParticipantsTempAsync(
        string rankingKey,
        string participantAvatarAddress,
        int seasonId,
        int score,
        int count
    )
    {
        double minScore = score - 100;
        double maxScore = score + 100;

        var participants = await _redis.SortedSetRangeByScoreWithScoresAsync(
            rankingKey,
            minScore,
            maxScore,
            Exclude.None,
            Order.Descending
        );

        var filteredParticipants = participants
            .Where(entry =>
            {
                var parts = entry.Element.ToString().Split(':');
                var address = parts[1];
                var participantSeasonId = int.Parse(parts[2]);

                return address != participantAvatarAddress && participantSeasonId == seasonId;
            })
            .Select(entry =>
            {
                var parts = entry.Element.ToString().Split(':');
                var address = new Address(parts[1]);
                var participantSeasonId = int.Parse(parts[2]);
                var participantScore = (int)entry.Score;

                return (
                    ParticipantAvatarAddress: address,
                    SeasonId: participantSeasonId,
                    Score: participantScore
                );
            })
            .ToList();

        var random = new Random();
        var randomParticipants = filteredParticipants
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();

        return randomParticipants;
    }
}

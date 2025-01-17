using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IRankingRepository
{
    Task UpdateScoreAsync(Address avatarAddress, int seasonId, int scoreChange);

    Task<int?> GetRankAsync(Address avatarAddress, int seasonId);

    Task<int?> GetScoreAsync(Address avatarAddress, int seasonId);

    Task<
        List<(int Rank, Address AvatarAddress, int SeasonId, int Score)>
    > GetRankingsWithPaginationAsync(int seasonId, int pageNumber, int pageSize);

    Task<
        List<(Address AvatarAddress, int SeasonId, int Score, int Rank)>
    > GetRandomParticipantsTempAsync(Address avatarAddress, int seasonId, int score, int count);
}

public class RankingRepository : IRankingRepository
{
    private const string RankingKeyPrefix = "ranking:season";
    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateScoreAsync(Address avatarAddress, int seasonId, int scoreChange)
    {
        await _redis.SortedSetIncrementAsync(
            $"{RankingKeyPrefix}:{seasonId}",
            $"participant:{avatarAddress.ToHex()}:{seasonId}",
            scoreChange
        );
    }

    public async Task<int?> GetRankAsync(Address avatarAddress, int seasonId)
    {
        var rank = await _redis.SortedSetRankAsync(
            $"{RankingKeyPrefix}:{seasonId}",
            $"participant:{avatarAddress.ToHex()}:{seasonId}",
            Order.Descending
        );
        return rank.HasValue ? (int)rank.Value + 1 : null;
    }

    public async Task<int?> GetScoreAsync(Address avatarAddress, int seasonId)
    {
        var score = await _redis.SortedSetScoreAsync(
            $"{RankingKeyPrefix}:{seasonId}",
            $"participant:{avatarAddress.ToHex()}:{seasonId}"
        );
        return score.HasValue ? (int)score.Value : null;
    }

    public async Task<
        List<(int Rank, Address AvatarAddress, int SeasonId, int Score)>
    > GetRankingsWithPaginationAsync(int seasonId, int pageNumber, int pageSize)
    {
        int start = (pageNumber - 1) * pageSize;
        int end = start + pageSize - 1;

        var rankedParticipants = await _redis.SortedSetRangeByRankWithScoresAsync(
            $"{RankingKeyPrefix}:{seasonId}",
            start,
            end,
            Order.Descending
        );

        return rankedParticipants
            .Select(
                (entry, index) =>
                {
                    var avatarAddress = entry.Element.ToString().Split(':')[1];
                    var seasonId = int.Parse(entry.Element.ToString().Split(':')[2]);
                    return (
                        Rank: start + index + 1,
                        AvatarAddress: new Address(avatarAddress),
                        SeasonId: seasonId,
                        Score: (int)entry.Score
                    );
                }
            )
            .ToList();
    }

    public async Task<
        List<(Address AvatarAddress, int SeasonId, int Score, int Rank)>
    > GetRandomParticipantsTempAsync(Address avatarAddress, int seasonId, int score, int count)
    {
        double minScore = score - 100;
        double maxScore = score + 100;

        var participants = await _redis.SortedSetRangeByScoreWithScoresAsync(
            $"{RankingKeyPrefix}:{seasonId}",
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

                return address != avatarAddress.ToHex() && participantSeasonId == seasonId;
            })
            .Select(entry =>
            {
                var parts = entry.Element.ToString().Split(':');
                var address = new Address(parts[1]);
                var participantSeasonId = int.Parse(parts[2]);
                var participantScore = (int)entry.Score;

                return (address, participantSeasonId, participantScore);
            })
            .ToList();

        var random = new Random();
        var randomParticipants = filteredParticipants
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();

        var result = new List<(Address AvatarAddress, int SeasonId, int Score, int Rank)>();

        foreach (var participant in randomParticipants)
        {
            var rank = await _redis.SortedSetRankAsync(
                $"{RankingKeyPrefix}:{seasonId}",
                $"participant:{participant.address.ToHex()}:{participant.participantSeasonId}",
                Order.Descending
            );

            result.Add(
                (
                    AvatarAddress: participant.address,
                    SeasonId: participant.participantSeasonId,
                    Score: participant.participantScore,
                    Rank: rank.HasValue ? (int)rank.Value + 1 : -1
                )
            );
        }

        return result;
    }
}

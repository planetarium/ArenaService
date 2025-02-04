using ArenaService.Exceptions;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IClanRankingRepository
{
    Task UpdateScoreAsync(int clanId, int seasonId, int roundId, int scoreChange);

    Task<int> GetRankAsync(int clanId, int seasonId, int roundId);

    Task<int> GetScoreAsync(int clanId, int seasonId, int roundId);

    Task<List<(int ClanId, int Score)>> GetScoresAsync(int seasonId, int roundId);

    Task CopyRoundDataAsync(int seasonId, int sourceRoundId, int targetRoundId, int roundInterval);

    Task InitRankingAsync(
        List<(int ClanId, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    );

    Task<List<(int ClanId, int Score, int Rank)>> GetTopClansAsync(
        int seasonId,
        int roundId,
        int topN
    );
}

public class ClanRankingRepository : IClanRankingRepository
{
    public const string ClanRankingKeyFormat = "season:{0}:round:{1}:ranking-clan";
    public const string ClanKeyFormat = "clan:{0}";

    private readonly IDatabase _redis;

    public ClanRankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateScoreAsync(int clanId, int seasonId, int roundId, int scoreChange)
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);
        string clanKey = string.Format(ClanKeyFormat, clanId);

        await _redis.SortedSetIncrementAsync(clanRankingKey, clanKey, scoreChange);
    }

    public async Task<int> GetRankAsync(int clanId, int seasonId, int roundId)
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);
        string clanKey = string.Format(ClanKeyFormat, clanId);

        var score = await _redis.SortedSetScoreAsync(clanRankingKey, clanKey);

        if (!score.HasValue)
        {
            throw new NotRankedException($"Clan {clanId} not found.");
        }

        var higherScoresCount = (
            await _redis.SortedSetRangeByScoreWithScoresAsync(
                clanRankingKey,
                double.PositiveInfinity,
                score.Value
            )
        ).Length;

        return higherScoresCount;
    }

    public async Task<int> GetScoreAsync(int clanId, int seasonId, int roundId)
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);
        string clanKey = string.Format(ClanKeyFormat, clanId);

        var score = await _redis.SortedSetScoreAsync(clanRankingKey, clanKey);
        return score.HasValue
            ? (int)score.Value
            : throw new NotRankedException($"Clan {clanId} not found.");
    }

    public async Task<List<(int ClanId, int Score)>> GetScoresAsync(int seasonId, int roundId)
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);

        var scores = await _redis.SortedSetRangeByRankWithScoresAsync(clanRankingKey);

        return scores
            .Select(
                (entry) =>
                {
                    var parts = entry.Element.ToString().Split(':');
                    var clanId = int.Parse(parts[1]!);

                    return (ClanId: clanId, Score: (int)entry.Score);
                }
            )
            .ToList();
    }

    public async Task InitRankingAsync(
        List<(int ClanId, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    )
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);

        foreach (var rankingEntry in rankingData)
        {
            string clanKey = string.Format(ClanKeyFormat, rankingEntry.ClanId);

            await _redis.SortedSetIncrementAsync(clanRankingKey, clanKey, rankingEntry.Score);
        }

        await _redis.KeyExpireAsync(clanRankingKey, TimeSpan.FromSeconds(roundInterval * 10 * 2));
    }

    public async Task CopyRoundDataAsync(
        int seasonId,
        int sourceRoundId,
        int targetRoundId,
        int roundInterval
    )
    {
        string sourceKey = string.Format(ClanRankingKeyFormat, seasonId, sourceRoundId);
        string targetKey = string.Format(ClanRankingKeyFormat, seasonId, targetRoundId);

        await _redis.SortedSetCombineAndStoreAsync(SetOperation.Union, targetKey, [sourceKey]);
        await _redis.KeyExpireAsync(targetKey, TimeSpan.FromSeconds(roundInterval * 10 * 2));
    }

    public async Task<List<(int ClanId, int Score, int Rank)>> GetTopClansAsync(
        int seasonId,
        int roundId,
        int topN
    )
    {
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);

        var topClans = await _redis.SortedSetRangeByRankWithScoresAsync(
            clanRankingKey,
            0,
            topN - 1,
            Order.Descending
        );

        return topClans
            .Select(
                (entry, i) =>
                {
                    var parts = entry.Element.ToString().Split(':');
                    var clanId = int.Parse(parts[1]!);

                    return (ClanId: clanId, Score: (int)entry.Score, Rank: i + 1);
                }
            )
            .ToList();
    }
}

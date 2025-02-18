using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface IAllClanRankingRepository
{
    Task UpdateScoreAsync(int clanId, int seasonId, int roundId, int scoreChange);

    Task<int> GetRankAsync(int clanId, int seasonId, int roundId);

    Task<int> GetScoreAsync(int clanId, int seasonId, int roundId);

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

public class AllClanRankingRepository : IAllClanRankingRepository
{
    public const int CacheRoundCount = 5;
    public const string ClanRankingKeyFormat = "season:{0}:round:{1}:ranking-clan";
    public const string ClanKeyFormat = "clan:{0}";
    public const string StatusKeyFormat = "season:{0}:round:{1}:ranking-clan:status";

    private readonly IDatabase _redis;

    public AllClanRankingRepository(IConnectionMultiplexer redis, int? databaseNumber = null)
    {
        if (databaseNumber is null)
        {
            _redis = redis.GetDatabase();
        }
        else
        {
            _redis = redis.GetDatabase(databaseNumber.Value);
        }
    }

    public async Task UpdateScoreAsync(int clanId, int seasonId, int roundId, int scoreChange)
    {
        await InsureRankingStatus(seasonId, roundId);

        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);
        string clanKey = string.Format(ClanKeyFormat, clanId);

        await _redis.SortedSetIncrementAsync(clanRankingKey, clanKey, scoreChange);
    }

    public async Task<int> GetRankAsync(int clanId, int seasonId, int roundId)
    {
        await InsureRankingStatus(seasonId, roundId);

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
        await InsureRankingStatus(seasonId, roundId);

        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);
        string clanKey = string.Format(ClanKeyFormat, clanId);

        var score = await _redis.SortedSetScoreAsync(clanRankingKey, clanKey);
        return score.HasValue
            ? (int)score.Value
            : throw new NotRankedException($"Clan {clanId} not found.");
    }

    public async Task<List<(int ClanId, int Score)>> GetScoresAsync(int seasonId, int roundId)
    {
        await InsureRankingStatus(seasonId, roundId);

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
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId);
        await _redis.StringSetAsync(statusKey, RankingStatus.INITIALIZING.ToString());
        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);

        foreach (var rankingEntry in rankingData)
        {
            string clanKey = string.Format(ClanKeyFormat, rankingEntry.ClanId);

            await _redis.SortedSetUpdateAsync(clanRankingKey, clanKey, rankingEntry.Score);
        }

        await _redis.KeyExpireAsync(
            clanRankingKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
        await _redis.StringSetAsync(
            statusKey,
            RankingStatus.DONE.ToString(),
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
    }

    public async Task CopyRoundDataAsync(
        int seasonId,
        int sourceRoundId,
        int targetRoundId,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, targetRoundId);
        await _redis.StringSetAsync(statusKey, RankingStatus.COPYING_IN_PROGRESS.ToString());
        string sourceKey = string.Format(ClanRankingKeyFormat, seasonId, sourceRoundId);
        string targetKey = string.Format(ClanRankingKeyFormat, seasonId, targetRoundId);

        await _redis.SortedSetCombineAndStoreAsync(SetOperation.Union, targetKey, [sourceKey]);
        await _redis.KeyExpireAsync(
            targetKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
        await _redis.StringSetAsync(
            statusKey,
            RankingStatus.DONE.ToString(),
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
    }

    public async Task<List<(int ClanId, int Score, int Rank)>> GetTopClansAsync(
        int seasonId,
        int roundId,
        int topN
    )
    {
        await InsureRankingStatus(seasonId, roundId);

        string clanRankingKey = string.Format(ClanRankingKeyFormat, seasonId, roundId);

        var topClans = await _redis.SortedSetRangeByRankWithScoresAsync(
            clanRankingKey,
            0,
            topN - 1,
            Order.Descending
        );

        var result = new List<(int ClanId, int Score, int Rank)>();

        int processedCount = 0;

        foreach (
            var group in topClans
                .Select(e => (Element: e.Element.ToString(), Score: (int)e.Score))
                .GroupBy(e => e.Score)
        )
        {
            int lastRank = processedCount + group.Count();

            foreach (var clan in group)
            {
                var parts = clan.Element.Split(':');
                var clanId = int.Parse(parts[1]!);
                result.Add((ClanId: clanId, clan.Score, Rank: lastRank));
            }

            processedCount += group.Count();
        }

        return result;
    }

    private async Task InsureRankingStatus(int seasonId, int roundId)
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId);
        var rankingStatus = await _redis.StringGetAsync(statusKey);
        if (rankingStatus != RankingStatus.DONE.ToString())
        {
            throw new CacheUnavailableException($"Ranking is {rankingStatus}");
        }
    }
}

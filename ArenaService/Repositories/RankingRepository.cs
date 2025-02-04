using ArenaService.Constants;
using ArenaService.Exceptions;
using Humanizer;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IRankingRepository
{
    Task UpdateScoreAsync(Address avatarAddress, int seasonId, int roundId, int scoreChange);

    Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundId);

    Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(int seasonId, int roundId);

    Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundId);

    Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    );

    Task CopyRoundDataAsync(int seasonId, int sourceRoundId, int targetRoundId, int roundInterval);
}

public class RankingRepository : IRankingRepository
{
    public const int CacheRoundCount = 3;
    public const string RankingKeyFormat = "season:{0}:round:{1}:ranking";
    public const string ParticipantKeyFormat = "participant:{0}";
    public const string StatusKeyFormat = "season:{0}:round:{1}:ranking:status";

    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange
    )
    {
        await InsureRankingStatus(seasonId, roundId);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        await _redis.SortedSetIncrementAsync(rankingKey, participantKey, scoreChange);
    }

    public async Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundId)
    {
        await InsureRankingStatus(seasonId, roundId);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        var score = await _redis.SortedSetScoreAsync(rankingKey, participantKey);

        if (!score.HasValue)
        {
            throw new NotRankedException($"Participant {avatarAddress} not found.");
        }

        var higherScoresCount = (
            await _redis.SortedSetRangeByScoreWithScoresAsync(
                rankingKey,
                double.PositiveInfinity,
                score.Value
            )
        ).Length;

        return higherScoresCount;
    }

    public async Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(
        int seasonId,
        int roundId
    )
    {
        await InsureRankingStatus(seasonId, roundId);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);

        var scores = await _redis.SortedSetRangeByRankWithScoresAsync(rankingKey);

        return scores
            .Select(
                (entry) =>
                {
                    var parts = entry.Element.ToString().Split(':');
                    var avatarAddress = new Address(parts[1]!);

                    return (AvatarAddress: avatarAddress, Score: (int)entry.Score);
                }
            )
            .ToList();
    }

    public async Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundId)
    {
        await InsureRankingStatus(seasonId, roundId);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        var score = await _redis.SortedSetScoreAsync(rankingKey, participantKey);
        return score.HasValue
            ? (int)score.Value
            : throw new NotRankedException($"Participant {avatarAddress} not found.");
    }

    public async Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId);
        await _redis.StringSetAsync(
            statusKey,
            RankingStatus.INITIALIZING.ToString()
        );
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);

        foreach (var rankingEntry in rankingData)
        {
            string participantKey = string.Format(
                ParticipantKeyFormat,
                rankingEntry.AvatarAddress.ToHex()
            );
            await _redis.SortedSetAddAsync(rankingKey, participantKey, rankingEntry.Score);
        }

        await _redis.KeyExpireAsync(
            rankingKey,
            TimeSpan.FromSeconds(
                roundInterval * ChainConstants.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
        await _redis.StringSetAsync(statusKey, RankingStatus.DONE.ToString(),
            TimeSpan.FromSeconds(
                roundInterval * ChainConstants.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            ));
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

        string sourceKey = string.Format(RankingKeyFormat, seasonId, sourceRoundId);
        string targetKey = string.Format(RankingKeyFormat, seasonId, targetRoundId);

        await _redis.SortedSetCombineAndStoreAsync(SetOperation.Union, targetKey, [sourceKey]);
        await _redis.KeyExpireAsync(
            targetKey,
            TimeSpan.FromSeconds(
                roundInterval * ChainConstants.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
        await _redis.StringSetAsync(
            statusKey,
            RankingStatus.DONE.ToString(),
            TimeSpan.FromSeconds(
                roundInterval * ChainConstants.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
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

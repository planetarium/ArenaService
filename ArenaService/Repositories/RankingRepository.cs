using ArenaService.Exceptions;
using Humanizer;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IRankingRepository
{
    Task UpdateScoreAsync(Address avatarAddress, int seasonId, int roundId, int scoreChange);

    Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundId);

    Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundId);

    Task CopyRoundDataAsync(int seasonId, int sourceRoundId, int targetRoundId);
}

public class RankingRepository : IRankingRepository
{
    public const string RankingKeyFormat = "season:{0}:round:{1}:ranking";
    public const string ParticipantKeyFormat = "participant:{0}";

    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task AddScoreAsync(Address avatarAddress, int seasonId, int roundId, int score)
    {
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        await _redis.SortedSetIncrementAsync(rankingKey, participantKey, score);
    }

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange
    )
    {
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        await _redis.SortedSetIncrementAsync(rankingKey, participantKey, scoreChange);
    }

    public async Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundId)
    {
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

    public async Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundId)
    {
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        var score = await _redis.SortedSetScoreAsync(rankingKey, participantKey);
        return score.HasValue
            ? (int)score.Value
            : throw new NotRankedException($"Participant {avatarAddress} not found.");
    }

    public async Task CopyRoundDataAsync(int seasonId, int sourceRoundId, int targetRoundId)
    {
        string sourceKey = string.Format(RankingKeyFormat, seasonId, sourceRoundId);
        string targetKey = string.Format(RankingKeyFormat, seasonId, targetRoundId);

        await _redis.SortedSetCombineAndStoreAsync(SetOperation.Union, targetKey, [sourceKey]);
    }
}

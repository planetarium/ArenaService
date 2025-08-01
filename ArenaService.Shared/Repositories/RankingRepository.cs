using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface IRankingRepository
{
    Task<string?> GetRankingStatus(int seasonId, int roundIndex);

    Task UpdateScoreAsync(Address avatarAddress, int seasonId, int roundIndex, int scoreChange);

    Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundIndex);

    Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(int seasonId, int roundIndex);

    Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundIndex);

    Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundIndex,
        int roundInterval
    );

    Task<Dictionary<int, (Address AvatarAddress, int Score)>> SelectBattleOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        bool isFirstRound
    );

    Task CopyRoundDataAsync(
        int seasonId,
        int sourceRoundIndex,
        int targetRoundIndex,
        int roundInterval
    );

    Task<int> GetRankingCountAsync(int seasonId, int roundIndex);

    Task ClearAllRankingCacheAsync();
}

public class RankingRepository : IRankingRepository
{
    private readonly Random _random = new();
    public const int CacheRoundCount = 5;
    public const string RankingKeyFormat = "season:{0}:round:{1}:ranking";
    public const string ParticipantKeyFormat = "participant:{0}";
    public const string StatusKeyFormat = "season:{0}:round:{1}:ranking:status";

    private readonly IDatabase _redis;

    public RankingRepository(IConnectionMultiplexer redis, int? databaseNumber = null)
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

    public async Task<string?> GetRankingStatus(int seasonId, int roundIndex)
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundIndex);
        var rankingStatus = await _redis.StringGetAsync(statusKey);
        return rankingStatus;
    }

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        int scoreChange
    )
    {
        await InsureRankingStatus(seasonId, roundIndex);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

        await _redis.SortedSetIncrementAsync(rankingKey, participantKey, scoreChange);
    }

    public async Task<int> GetRankAsync(Address avatarAddress, int seasonId, int roundIndex)
    {
        await InsureRankingStatus(seasonId, roundIndex);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

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
        int roundIndex
    )
    {
        await InsureRankingStatus(seasonId, roundIndex);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);

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

    public async Task<int> GetRankingCountAsync(int seasonId, int roundIndex)
    {
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);

        int totalRankingCount = (int)await _redis.SortedSetLengthAsync(rankingKey);

        return totalRankingCount;
    }

    public async Task<int> GetScoreAsync(Address avatarAddress, int seasonId, int roundIndex)
    {
        await InsureRankingStatus(seasonId, roundIndex);

        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

        var score = await _redis.SortedSetScoreAsync(rankingKey, participantKey);
        return score.HasValue
            ? (int)score.Value
            : throw new NotRankedException($"Participant {avatarAddress} not found.");
    }

    public async Task<
        Dictionary<int, (Address AvatarAddress, int Score)>
    > SelectBattleOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        bool isFirstRound
    )
    {
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

        int totalRankingCount = (int)await _redis.SortedSetLengthAsync(rankingKey);

        if (totalRankingCount <= 40)
        {
            throw new CalcAOFailedException(
                $"Total ranking count is under 40 - {totalRankingCount}"
            );
        }
        if (!isFirstRound && totalRankingCount > 1000)
        {
            totalRankingCount = 1000;
        }

        long? myRank = await _redis.SortedSetRankAsync(
            rankingKey,
            participantKey,
            Order.Descending
        );
        if (myRank == null)
        {
            throw new CalcAOFailedException($"Player {avatarAddress} is not ranked.");
        }

        var selectedOpponents = new Dictionary<int, (Address AvatarAddress, int Score)>();
        var usedIndices = new HashSet<int> { (int)myRank.Value };

        foreach (var group in OpponentGroupConstants.Groups)
        {
            var minIndex = (int)Math.Floor(group.Value.MinRange * totalRankingCount);
            var maxIndex = (int)Math.Floor(group.Value.MaxRange * totalRankingCount);

            if (maxIndex >= totalRankingCount)
            {
                maxIndex = totalRankingCount - 1;
            }
            if (minIndex <= 0)
            {
                minIndex = 0;
            }

            var candidateIndices = Enumerable
                .Range(minIndex, maxIndex - minIndex)
                .Where(i => !usedIndices.Contains(i))
                .ToList();

            if (candidateIndices.Count > 0)
            {
                var selectedIndex = candidateIndices[_random.Next(candidateIndices.Count)];
                usedIndices.Add(selectedIndex);

                var opponentEntry = (
                    await _redis.SortedSetRangeByRankWithScoresAsync(
                        rankingKey,
                        selectedIndex,
                        selectedIndex,
                        Order.Descending
                    )
                ).SingleOrDefault();

                if (opponentEntry.Element.IsNull)
                {
                    throw new CalcAOFailedException(
                        $"Failed to retrieve player at rank {selectedIndex + 1}."
                    );
                }

                var parts = opponentEntry.ToString().Split(':');
                var opponentAvatarAddress = new Address(parts[1]!);
                int opponentScore = (int)opponentEntry.Score;

                selectedOpponents[group.Key] = (opponentAvatarAddress, opponentScore);
            }
        }

        if (selectedOpponents.Count != 5)
        {
            throw new CalcAOFailedException($"AO Count is {selectedOpponents.Count}");
        }

        return selectedOpponents;
    }

    public async Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundIndex,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundIndex);
        await _redis.StringSetAsync(statusKey, RankingStatus.INITIALIZING.ToString());
        string rankingKey = string.Format(RankingKeyFormat, seasonId, roundIndex);

        foreach (var rankingEntry in rankingData)
        {
            string participantKey = string.Format(
                ParticipantKeyFormat,
                rankingEntry.AvatarAddress.ToHex().ToLower()
            );
            await _redis.SortedSetAddAsync(rankingKey, participantKey, rankingEntry.Score);
        }

        await _redis.KeyExpireAsync(
            rankingKey,
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
        int sourceRoundIndex,
        int targetRoundIndex,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, targetRoundIndex);
        await _redis.StringSetAsync(statusKey, RankingStatus.COPYING_IN_PROGRESS.ToString());

        string sourceKey = string.Format(RankingKeyFormat, seasonId, sourceRoundIndex);
        string targetKey = string.Format(RankingKeyFormat, seasonId, targetRoundIndex);

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

    private async Task InsureRankingStatus(int seasonId, int roundIndex)
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundIndex);
        var rankingStatus = await _redis.StringGetAsync(statusKey);
        if (rankingStatus != RankingStatus.DONE.ToString())
        {
            throw new CacheUnavailableException($"Ranking is {rankingStatus}");
        }
    }

    public async Task ClearAllRankingCacheAsync()
    {
        var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: "season:*:round:*:ranking");

        foreach (var key in keys)
        {
            await _redis.KeyDeleteAsync(key);
        }

        var statusKeys = server.Keys(pattern: "season:*:round:*:ranking:status");
        foreach (var key in statusKeys)
        {
            await _redis.KeyDeleteAsync(key);
        }
    }
}

using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface IGroupRankingRepository
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int scoreChange,
        int roundInterval
    );

    Task<Dictionary<int, (Address AvatarAddress, int Score)>> SelectBattleOpponentsAsync(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score
    );

    Task<Dictionary<int, (long Min, long Max)>> CalcGroupRankRange(
        int seasonId,
        int roundId,
        long rank
    );

    Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    );

    Task CopyRoundDataAsync(int seasonId, int sourceRoundId, int targetRoundId, int roundInterval);
}

public class GroupRankingRepository : IGroupRankingRepository
{
    public const int CacheRoundCount = 5;
    public const string ParticipantKeyFormat = "participant:{0}";
    public const string GroupedRankingKeyFormat = "season:{0}:round:{1}:ranking-group";
    public const string GroupRankingMemberKeyFormat = "group:{0}";
    public const string GroupKeyFormat = "season:{0}:round:{1}:group:{2}";
    public const string StatusKeyFormat = "season:{0}:round:{1}:ranking-group:status";

    public readonly Dictionary<int, List<int>> FallbackGroups = new Dictionary<int, List<int>>
    {
        {
            1,
            new List<int> { 2, 3, 4, 5 }
        },
        {
            2,
            new List<int> { 3, 4, 5, 1 }
        },
        {
            3,
            new List<int> { 4, 5, 2, 1 }
        },
        {
            4,
            new List<int> { 5, 3, 2, 1 }
        },
        {
            5,
            new List<int> { 4, 3, 2, 1 }
        },
    };

    private readonly IDatabase _redis;

    public GroupRankingRepository(IConnectionMultiplexer redis, int? databaseNumber = null)
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

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int nextScore,
        int roundInterval
    )
    {
        await InsureRankingStatus(seasonId, roundId);

        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);

        string prevGroupRankingMemberKey = string.Format(GroupRankingMemberKeyFormat, prevScore);
        string changedGroupRankingMemberKey = string.Format(GroupRankingMemberKeyFormat, nextScore);

        string changedGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, nextScore);
        string prevGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, prevScore);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        await _redis.HashDeleteAsync(prevGroupKey, participantKey);
        await _redis.HashSetAsync(changedGroupKey, participantKey, nextScore);
        await _redis.KeyExpireAsync(
            changedGroupKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
        await _redis.SortedSetAddAsync(groupRankingKey, changedGroupRankingMemberKey, nextScore);

        bool isPrevGroupEmpty = await _redis.HashLengthAsync(prevGroupKey) == 0;
        if (isPrevGroupEmpty)
        {
            await _redis.SortedSetRemoveAsync(groupRankingKey, prevGroupRankingMemberKey);
        }
    }

    public async Task<
        Dictionary<int, (Address AvatarAddress, int Score)>
    > SelectBattleOpponentsAsync(int seasonId, int roundId, Address avatarAddress, int score)
    {
        await InsureRankingStatus(seasonId, roundId);

        string myKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());
        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);

        var opponents = new Dictionary<int, (Address AvatarAddress, int Score)>();
        var selectedAvatars = new HashSet<Address>();

        long? myRankIndex = await _redis.SortedSetRankAsync(
            groupRankingKey,
            string.Format(GroupRankingMemberKeyFormat, score),
            Order.Descending
        );

        if (!myRankIndex.HasValue)
        {
            throw new NotRankedException("Current score group not found in ranking.");
        }

        var adjustedRanking = myRankIndex + 1;

        var groupRangeDict = await CalcGroupRankRange(seasonId, roundId, adjustedRanking.Value);

        foreach (var (groupId, (minRange, maxRange)) in groupRangeDict)
        {
            var foundOpponent = await TrySelectOpponentFromRanges(
                groupRankingKey,
                minRange,
                maxRange,
                selectedAvatars,
                seasonId,
                roundId,
                myKey
            );

            if (foundOpponent != null)
            {
                opponents[groupId] = foundOpponent.Value;
                selectedAvatars.Add(foundOpponent.Value.AvatarAddress);
            }
            else
            {
                var fallbackGroupIds = FallbackGroups[groupId];
                foreach (var fallbackGroupId in fallbackGroupIds)
                {
                    var (fallbackMin, fallbackMax) = groupRangeDict[fallbackGroupId];

                    foundOpponent = await TrySelectOpponentFromRanges(
                        groupRankingKey,
                        fallbackMin,
                        fallbackMax,
                        selectedAvatars,
                        seasonId,
                        roundId,
                        myKey
                    );

                    if (foundOpponent != null)
                    {
                        opponents[groupId] = foundOpponent.Value;
                        selectedAvatars.Add(foundOpponent.Value.AvatarAddress);
                        break;
                    }
                }

                if (foundOpponent == null)
                {
                    // fallback group에서도 찾지 못한 경우 등수를 낮춰 탐색합니다. (+100까지)
                    for (int i = 1; i <= 100; i++)
                    {
                        var nextRankGroupRangeDict = await CalcGroupRankRange(
                            seasonId,
                            roundId,
                            adjustedRanking.Value + i
                        );

                        var (nextRankGroupMinRange, nextRankGroupMaxRange) = nextRankGroupRangeDict[
                            groupId
                        ];

                        if (nextRankGroupMinRange == minRange || nextRankGroupMaxRange == maxRange)
                        {
                            continue;
                        }

                        foundOpponent = await TrySelectOpponentFromRanges(
                            groupRankingKey,
                            nextRankGroupMinRange,
                            nextRankGroupMaxRange,
                            selectedAvatars,
                            seasonId,
                            roundId,
                            myKey
                        );

                        if (foundOpponent != null)
                        {
                            opponents[groupId] = foundOpponent.Value;
                            selectedAvatars.Add(foundOpponent.Value.AvatarAddress);
                            break;
                        }
                    }
                }
            }
        }

        return opponents;
    }

    private async Task<(Address AvatarAddress, int Score)?> TrySelectOpponentFromRanges(
        string groupRankingKey,
        long minRange,
        long maxRange,
        HashSet<Address> selectedAvatars,
        int seasonId,
        int roundId,
        string myKey
    )
    {
        var availableGroupKeys = (
            await _redis.SortedSetRangeByRankAsync(
                groupRankingKey,
                minRange - 1,
                maxRange - 1,
                Order.Descending
            )
        ).ToList();

        while (availableGroupKeys.Any())
        {
            Random random = new Random();
            int selectedGroupIndex = random.Next(availableGroupKeys.Count);
            var selectedGroupKey = availableGroupKeys[selectedGroupIndex];

            var groupParticipants = await _redis.HashGetAllAsync(
                $"season:{seasonId}:round:{roundId}:{selectedGroupKey}"
            );

            var filteredParticipants = groupParticipants
                .Where(p => !p.Name.Equals(myKey))
                .Select(p =>
                {
                    var parts = p.ToString().Split(':');
                    return (AvatarAddress: new Address(parts[1]), Score: int.Parse(p.Value!));
                })
                .Where(p => !selectedAvatars.Contains(p.AvatarAddress))
                .ToArray();

            if (filteredParticipants.Any())
            {
                int selectedParticipantIndex = random.Next(filteredParticipants.Length);
                return filteredParticipants[selectedParticipantIndex];
            }

            availableGroupKeys.RemoveAt(selectedGroupIndex);
        }

        return null;
    }

    public async Task<Dictionary<int, (long Min, long Max)>> CalcGroupRankRange(
        int seasonId,
        int roundId,
        long rank
    )
    {
        await InsureRankingStatus(seasonId, roundId);

        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);

        long totalRankings = await _redis.SortedSetLengthAsync(groupRankingKey);
        if (totalRankings == 0)
        {
            throw new NotRankedException("TotalRankings is 0");
        }

        Dictionary<int, (long Min, long Max)> result = new Dictionary<int, (long Min, long Max)>();
        foreach (var (groupId, (minRange, maxRange, _, _)) in OpponentGroupConstants.Groups)
        {
            long min = Math.Max(1, (long)Math.Ceiling(rank * minRange));
            min = Math.Min(totalRankings, min);
            long max = Math.Min(totalRankings, (long)Math.Ceiling(rank * maxRange));

            result[groupId] = (min, max);
        }

        return result;
    }

    public async Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int seasonId,
        int roundId,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId);
        await _redis.StringSetAsync(statusKey, RankingStatus.INITIALIZING.ToString());
        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);

        foreach (var rankingEntry in rankingData)
        {
            string groupRankingMemberKey = string.Format(
                GroupRankingMemberKeyFormat,
                rankingEntry.Score
            );
            await _redis.SortedSetAddAsync(
                groupRankingKey,
                groupRankingMemberKey,
                rankingEntry.Score
            );
        }

        await _redis.KeyExpireAsync(
            groupRankingKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );

        foreach (var rankingEntry in rankingData)
        {
            string groupKey = string.Format(GroupKeyFormat, seasonId, roundId, rankingEntry.Score);
            string participantKey = string.Format(
                ParticipantKeyFormat,
                rankingEntry.AvatarAddress.ToHex()
            );

            await _redis.HashSetAsync(groupKey, participantKey, rankingEntry.Score);
            await _redis.KeyExpireAsync(
                groupKey,
                TimeSpan.FromSeconds(
                    roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
                )
            );
        }
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

        string sourceGroupRankingKey = string.Format(
            GroupedRankingKeyFormat,
            seasonId,
            sourceRoundId
        );
        string targetGroupRankingKey = string.Format(
            GroupedRankingKeyFormat,
            seasonId,
            targetRoundId
        );

        await _redis.SortedSetCombineAndStoreAsync(
            SetOperation.Union,
            targetGroupRankingKey,
            [sourceGroupRankingKey]
        );
        await _redis.KeyExpireAsync(
            targetGroupRankingKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );

        var sourceGroups = await _redis.SortedSetRangeByRankAsync(sourceGroupRankingKey);
        foreach (var sourceGroup in sourceGroups)
        {
            var parts = sourceGroup.ToString().Split(":");
            var score = int.Parse(parts[1]);
            string sourceGroupKey = string.Format(GroupKeyFormat, seasonId, sourceRoundId, score);
            string targetGroupKey = string.Format(GroupKeyFormat, seasonId, targetRoundId, score);

            var groupParticipants = await _redis.HashGetAllAsync(sourceGroupKey);
            if (groupParticipants.Length > 0)
            {
                await _redis.HashSetAsync(targetGroupKey, groupParticipants);
                await _redis.KeyExpireAsync(
                    targetGroupKey,
                    TimeSpan.FromSeconds(
                        roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
                    )
                );
            }
        }
        await _redis.StringSetAsync(
            statusKey,
            RankingStatus.DONE.ToString(),
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
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

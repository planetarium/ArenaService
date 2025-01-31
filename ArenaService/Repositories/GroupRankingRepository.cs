using ArenaService.Constants;
using ArenaService.Exceptions;
using Humanizer;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IGroupRankingRepository
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int scoreChange
    );

    Task<Dictionary<int, (Address AvatarAddress, int Score)?>> SelectBattleOpponentsAsync(
        Address avatarAddress,
        int score,
        int seasonId,
        int roundId
    );
}

public class GroupRankingRepository : IGroupRankingRepository
{
    public const string ParticipantKeyFormat = "participant:{0}";
    public const string GroupedRankingKeyFormat = "season:{0}:round:{1}:ranking-group";
    public const string GroupKeyFormat = "season:{0}:round:{1}:group:{2}";

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
        int nextScore
    )
    {
        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        string changedGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, nextScore);
        string prevGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, prevScore);

        await _redis.HashDeleteAsync(prevGroupKey, participantKey);
        await _redis.HashSetAsync(changedGroupKey, participantKey, nextScore);
        await _redis.SortedSetAddAsync(groupRankingKey, changedGroupKey, nextScore);

        bool isPrevGroupEmpty = await _redis.HashLengthAsync(prevGroupKey) == 0;
        if (isPrevGroupEmpty)
        {
            await _redis.SortedSetRemoveAsync(groupRankingKey, prevGroupKey);
        }
    }

    public async Task<
        Dictionary<int, (Address AvatarAddress, int Score)?>
    > SelectBattleOpponentsAsync(Address avatarAddress, int score, int seasonId, int roundId)
    {
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());
        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);

        long totalRankings = await _redis.SortedSetLengthAsync(groupRankingKey);
        if (totalRankings == 0)
        {
            return new Dictionary<int, (Address AvatarAddress, int Score)?>();
        }

        long? myRank = await _redis.SortedSetRankAsync(
            groupRankingKey,
            string.Format(GroupKeyFormat, seasonId, roundId, score)
        );

        if (!myRank.HasValue)
        {
            throw new KeyNotFoundException("Current score group not found in ranking.");
        }
        myRank += 1;

        var opponents = new Dictionary<int, (Address AvatarAddress, int Score)?>();

        foreach (var (groupId, (minRange, maxRange, _, _)) in OpponentGroupConstants.Groups)
        {
            var (minRank, maxRank) = CalculateRange(
                myRank.Value,
                minRange,
                maxRange,
                totalRankings
            );

            var groupKeys = await _redis.SortedSetRangeByRankAsync(
                groupRankingKey,
                totalRankings - maxRank,
                totalRankings - minRank
            );

            if (!groupKeys.Any())
            {
                opponents[groupId] = null;
                continue;
            }

            Random random = new Random();
            int selectedGroupIndex = random.Next(groupKeys.Count());
            var selectedGroupKey = groupKeys[selectedGroupIndex];

            var groupParticipants = await _redis.HashGetAllAsync(selectedGroupKey.ToString());

            var filteredParticipants = groupParticipants
                .Where(p => !p.Name.Equals(participantKey))
                .ToArray();

            if (!filteredParticipants.Any())
            {
                opponents[groupId] = null;
                continue;
            }

            int selectedParticipantIndex = random.Next(filteredParticipants.Count());
            var selectedParticipant = filteredParticipants[selectedParticipantIndex];

            var parts = selectedParticipant.ToString().Split(':');

            opponents[groupId] = (new Address(parts[1]), int.Parse(selectedParticipant.Value!));
        }

        return opponents;
    }

    private static (long Min, long Max) CalculateRange(
        long rank,
        double minMultiplier,
        double maxMultiplier,
        long totalRankings
    )
    {
        var adjustedRanking = totalRankings - rank + 1;

        long min = Math.Max(1, (long)(adjustedRanking * minMultiplier));
        long max = Math.Min(totalRankings, (long)(adjustedRanking * maxMultiplier));

        return (min, max);
    }
}

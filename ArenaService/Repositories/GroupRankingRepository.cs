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
        int scoreChange
    )
    {
        string groupRankingKey = string.Format(GroupedRankingKeyFormat, seasonId, roundId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex());

        string changedGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, scoreChange);
        string prevGroupKey = string.Format(GroupKeyFormat, seasonId, roundId, prevScore);

        // 1. 기존 점수 그룹 제거
        await _redis.HashDeleteAsync(prevGroupKey, participantKey);

        // 2. 새로운 점수 그룹에 추가
        await _redis.HashSetAsync(changedGroupKey, participantKey, scoreChange);

        // 3. 새로운 점수 그룹을 그룹 랭킹에 추가
        await _redis.SortedSetAddAsync(groupRankingKey, changedGroupKey, scoreChange);

        // 4. 만약 1번에서 점수 그룹에 제거 되었다면 랭킹에서도 제외해야함
        bool isPrevGroupEmpty = await _redis.HashLengthAsync(prevGroupKey) == 0;
        if (isPrevGroupEmpty)
        {
            await _redis.SortedSetRemoveAsync(groupRankingKey, prevGroupKey);
        }
    }

    public async Task<
        Dictionary<int, (Address AvatarAddress, int Score, int Rank)?>
    > SelectBattleOpponentsAsync(Address avatarAddress, int seasonId, int roundId)
    {
        return new Dictionary<int, (Address AvatarAddress, int Score, int Rank)?>
        {
            { 1, (new Address(), 1, 1) },
            { 2, (new Address(), 1, 1) },
            { 3, (new Address(), 1, 1) },
            { 4, (new Address(), 1, 1) },
            { 5, (new Address(), 1, 1) },
        };
    }
}

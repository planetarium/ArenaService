using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface IClanRankingRepository
{
    Task UpdateScoreAsync(
        int clanId,
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        int scoreChange
    );

    Task<int> GetRankAsync(int clanId, Address avatarAddress, int seasonId, int roundIndex);

    Task<List<int>> GetClansAsync(int seasonId, int roundIndex);

    Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(
        int clanId,
        int seasonId,
        int roundIndex
    );

    Task CopyRoundDataAsync(
        int clanId,
        int seasonId,
        int sourceRoundIndex,
        int targetRoundIndex,
        int roundInterval
    );

    Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int clanId,
        int seasonId,
        int roundIndex,
        int roundInterval
    );

    Task<List<(int Score, int Rank)>> GetTopClansAsync(
        int clanId,
        int seasonId,
        int roundIndex,
        int topN = 10
    );
}

public class ClanRankingRepository : IClanRankingRepository
{
    public const int CacheRoundCount = 5;
    public const string ClanRankingFormat = "season:{0}:round:{1}:clan:{2}:ranking";
    public const string ParticipantKeyFormat = "participant:{0}";
    public const string StatusKeyFormat = "season:{0}:round:{1}:clan:{2}:ranking:status";
    public const string ClansKeyFormat = "season:{0}:round:{1}:clans";

    private readonly IDatabase _redis;

    public ClanRankingRepository(IConnectionMultiplexer redis, int? databaseNumber = null)
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
        int clanId,
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        int scoreChange
    )
    {
        await InsureRankingStatus(seasonId, roundIndex, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundIndex, clanId);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

        await _redis.SortedSetIncrementAsync(clanRankingKey, participantKey, scoreChange);
    }

    public async Task<int> GetRankAsync(
        int clanId,
        Address avatarAddress,
        int seasonId,
        int roundIndex
    )
    {
        await InsureRankingStatus(seasonId, roundIndex, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundIndex, clanId);
        string participantKey = string.Format(
            ParticipantKeyFormat,
            avatarAddress.ToHex().ToLower()
        );

        var rank = await _redis.SortedSetRankAsync(
            clanRankingKey,
            participantKey,
            Order.Descending
        );

        if (!rank.HasValue)
        {
            throw new NotRankedException($"Participant {avatarAddress} not found.");
        }

        return (int)rank + 1;
    }

    public async Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(
        int clanId,
        int seasonId,
        int roundIndex
    )
    {
        await InsureRankingStatus(seasonId, roundIndex, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundIndex, clanId);

        var scores = await _redis.SortedSetRangeByRankWithScoresAsync(clanRankingKey);

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

    public async Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int clanId,
        int seasonId,
        int roundIndex,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundIndex, clanId);
        await _redis.StringSetAsync(statusKey, RankingStatus.INITIALIZING.ToString());
        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundIndex, clanId);

        foreach (var rankingEntry in rankingData)
        {
            string participantKey = string.Format(
                ParticipantKeyFormat,
                rankingEntry.AvatarAddress.ToHex().ToLower()
            );

            await _redis.SortedSetIncrementAsync(
                clanRankingKey,
                participantKey,
                rankingEntry.Score
            );
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

        string clansKey = string.Format(ClansKeyFormat, seasonId, roundIndex);
        await _redis.SetAddAsync(clansKey, clanId.ToString());
        await _redis.KeyExpireAsync(
            clansKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
    }

    public async Task CopyRoundDataAsync(
        int clanId,
        int seasonId,
        int sourceRoundIndex,
        int targetRoundIndex,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, targetRoundIndex, clanId);
        await _redis.StringSetAsync(statusKey, RankingStatus.COPYING_IN_PROGRESS.ToString());
        string sourceKey = string.Format(ClanRankingFormat, seasonId, sourceRoundIndex, clanId);
        string targetKey = string.Format(ClanRankingFormat, seasonId, targetRoundIndex, clanId);

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

        string clansKey = string.Format(ClansKeyFormat, seasonId, targetRoundIndex);

        var clans = await GetClansAsync(seasonId, sourceRoundIndex);
        foreach (var existsClanId in clans)
        {
            await _redis.SetAddAsync(clansKey, existsClanId.ToString());
        }

        await _redis.KeyExpireAsync(
            clansKey,
            TimeSpan.FromSeconds(
                roundInterval * ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * CacheRoundCount
            )
        );
    }

    public async Task<List<int>> GetClansAsync(int seasonId, int roundIndex)
    {
        string clansKey = string.Format(ClansKeyFormat, seasonId, roundIndex);
        var clanIds = await _redis.SetMembersAsync(clansKey);
        return clanIds.Select(id => int.Parse(id.ToString())).ToList();
    }

    public async Task<List<(int Score, int Rank)>> GetTopClansAsync(
        int clanId,
        int seasonId,
        int roundIndex,
        int topN
    )
    {
        await InsureRankingStatus(seasonId, roundIndex, clanId);

        string clanRankingPerClanKey = string.Format(ClanRankingFormat, seasonId, roundIndex, clanId);

        var topParticipants = await _redis.SortedSetRangeByRankWithScoresAsync(
            clanRankingPerClanKey,
            0,
            topN - 1,
            Order.Descending
        );

        return topParticipants
            .Select(
                (entry, i) =>
                {
                    return (Score: (int)entry.Score, Rank: i + 1);
                }
            )
            .ToList();
    }

    private async Task InsureRankingStatus(int seasonId, int roundIndex, int clanId)
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundIndex, clanId);
        var rankingStatus = await _redis.StringGetAsync(statusKey);
        if (rankingStatus != RankingStatus.DONE.ToString())
        {
            throw new CacheUnavailableException($"Ranking is {rankingStatus}");
        }
    }
}

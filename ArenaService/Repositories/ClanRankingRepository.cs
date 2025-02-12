using ArenaService.Constants;
using ArenaService.Exceptions;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface IClanRankingRepository
{
    Task UpdateScoreAsync(
        int clanId,
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange
    );

    Task<int> GetRankAsync(int clanId, Address avatarAddress, int seasonId, int roundId);

    Task<List<int>> GetClansAsync(int seasonId, int roundId);

    Task<List<(Address AvatarAddress, int Score)>> GetScoresAsync(
        int clanId,
        int seasonId,
        int roundId
    );

    Task CopyRoundDataAsync(
        int clanId,
        int seasonId,
        int sourceRoundId,
        int targetRoundId,
        int roundInterval
    );

    Task InitRankingAsync(
        List<(Address AvatarAddress, int Score)> rankingData,
        int clanId,
        int seasonId,
        int roundId,
        int roundInterval
    );

    Task<List<(int Score, int Rank)>> GetTopClansAsync(
        int clanId,
        int seasonId,
        int roundId,
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
        int roundId,
        int scoreChange
    )
    {
        await InsureRankingStatus(seasonId, roundId, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundId, clanId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex().ToLower());

        await _redis.SortedSetIncrementAsync(clanRankingKey, participantKey, scoreChange);
    }

    public async Task<int> GetRankAsync(
        int clanId,
        Address avatarAddress,
        int seasonId,
        int roundId
    )
    {
        await InsureRankingStatus(seasonId, roundId, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundId, clanId);
        string participantKey = string.Format(ParticipantKeyFormat, avatarAddress.ToHex().ToLower());

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
        int roundId
    )
    {
        await InsureRankingStatus(seasonId, roundId, clanId);

        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundId, clanId);

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
        int roundId,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId, clanId);
        await _redis.StringSetAsync(statusKey, RankingStatus.INITIALIZING.ToString());
        string clanRankingKey = string.Format(ClanRankingFormat, seasonId, roundId, clanId);

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

        string clansKey = string.Format(ClansKeyFormat, seasonId, roundId);
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
        int sourceRoundId,
        int targetRoundId,
        int roundInterval
    )
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, targetRoundId, clanId);
        await _redis.StringSetAsync(statusKey, RankingStatus.COPYING_IN_PROGRESS.ToString());
        string sourceKey = string.Format(ClanRankingFormat, seasonId, sourceRoundId, clanId);
        string targetKey = string.Format(ClanRankingFormat, seasonId, targetRoundId, clanId);

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

        string clansKey = string.Format(ClansKeyFormat, seasonId, targetRoundId);

        var clans = await GetClansAsync(seasonId, sourceRoundId);
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

    public async Task<List<int>> GetClansAsync(int seasonId, int roundId)
    {
        string clansKey = string.Format(ClansKeyFormat, seasonId, roundId);
        var clanIds = await _redis.SetMembersAsync(clansKey);
        return clanIds.Select(id => int.Parse(id.ToString())).ToList();
    }

    public async Task<List<(int Score, int Rank)>> GetTopClansAsync(
        int clanId,
        int seasonId,
        int roundId,
        int topN
    )
    {
        await InsureRankingStatus(seasonId, roundId, clanId);

        string clanRankingPerClanKey = string.Format(ClanRankingFormat, seasonId, roundId, clanId);

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

    private async Task InsureRankingStatus(int seasonId, int roundId, int clanId)
    {
        string statusKey = string.Format(StatusKeyFormat, seasonId, roundId, clanId);
        var rankingStatus = await _redis.StringGetAsync(statusKey);
        if (rankingStatus != RankingStatus.DONE.ToString())
        {
            throw new CacheUnavailableException($"Ranking is {rankingStatus}");
        }
    }
}

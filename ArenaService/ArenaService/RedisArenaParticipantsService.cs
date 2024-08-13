using System.Text.Json;
using Libplanet.Crypto;
using StackExchange.Redis;

namespace ArenaService;

public class RedisArenaParticipantsService : IRedisArenaParticipantsService
{
    public const string SeasonKey = "season";

    private readonly IDatabase _db;

    public RedisArenaParticipantsService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<List<ArenaParticipant>> GetArenaParticipantsAsync(string key)
    {
        RedisValue result = await _db.StringGetAsync(key);
        if (result.IsNull)
        {
            return new List<ArenaParticipant>();
        }

        return JsonSerializer.Deserialize<List<ArenaParticipant>>(result.ToString())!;
    }

    public async Task SetArenaParticipantsAsync(string key, List<ArenaParticipant> value, TimeSpan? expiry = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, serialized, expiry);
    }

    public async Task<string> GetSeasonKeyAsync()
    {
        RedisValue result = await _db.StringGetAsync(SeasonKey);
        if (result.IsNull)
        {
            throw new KeyNotFoundException();
        }

        return result.ToString();
    }

    public async Task SetSeasonAsync(string value, TimeSpan? expiry = null)
    {
        await _db.StringSetAsync(SeasonKey, value, expiry);
    }

    public async Task<List<(Address avatarAddr, int score, int rank)>> GetAvatarAddrAndScoresWithRank(string key)
    {
        RedisValue result = await _db.StringGetAsync(key);
        if (result.IsNull)
        {
            return new List<(Address avatarAddr, int score, int rank)>();
        }

        return JsonSerializer.Deserialize<List<(Address avatarAddr, int score, int rank)>>(result.ToString())!;
    }

    public async Task SetAvatarAddrAndScoresWithRank(string key, List<(Address avatarAddr, int score, int rank)> value, TimeSpan? expiry = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, serialized, expiry);
    }
}

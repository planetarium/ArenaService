using System.Text.Json;
using StackExchange.Redis;
using ArenaService.Models;

namespace ArenaService;

public class RedisArenaParticipantsService(IConnectionMultiplexer redis)
    : IRedisArenaParticipantsService
{
    public const string SeasonKey = "season";

    private readonly IDatabase _db = redis.GetDatabase();

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

    public async Task<List<AvatarAddressAndScore>> GetAvatarAddrAndScores(string key)
    {
        RedisValue result = await _db.StringGetAsync(key);
        if (result.IsNull)
        {
            return new List<AvatarAddressAndScore>();
        }

        return JsonSerializer.Deserialize<List<AvatarAddressAndScore>>(result.ToString())!;
    }

    public async Task SetAvatarAddrAndScores(string key, List<AvatarAddressAndScore> value, TimeSpan? expiry = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, serialized, expiry);
    }
}

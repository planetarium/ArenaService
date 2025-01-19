using System.Text.Json;
using StackExchange.Redis;

namespace ArenaService.Repositories;

public interface ISeasonCacheRepository
{
    Task<long?> GetBlockIndexAsync();
    Task<(int Id, long StartBlock, long EndBlock)?> GetSeasonAsync();
    Task<(int Id, long StartBlock, long EndBlock)?> GetRoundAsync();
    Task SetBlockIndexAsync(long blockIndex);
    Task SetSeasonAsync(int seasonId, long startBlock, long endBlock);
    Task SetRoundAsync(int roundId, long startBlock, long endBlock);
}

public class SeasonCacheRepository : ISeasonCacheRepository
{
    private readonly IDatabase _redis;
    private const string PREFIX = "season_cache";
    private const string BlockIndexKey = "block_index";
    private const string SeasonKey = "season";
    private const string RoundKey = "round";

    public SeasonCacheRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<long?> GetBlockIndexAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{BlockIndexKey}");
        return value.HasValue ? long.Parse(value) : null;
    }

    public async Task<(int Id, long StartBlock, long EndBlock)?> GetSeasonAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{SeasonKey}");
        if (!value.HasValue)
        {
            return null;
        }

        var seasonData = JsonSerializer.Deserialize<CachedSeason>(value);
        return seasonData != null
            ? (seasonData.Id, seasonData.StartBlock, seasonData.EndBlock)
            : null;
    }

    public async Task<(int Id, long StartBlock, long EndBlock)?> GetRoundAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{RoundKey}");
        if (!value.HasValue)
        {
            return null;
        }

        var roundData = JsonSerializer.Deserialize<CachedRound>(value);
        return roundData != null ? (roundData.Id, roundData.StartBlock, roundData.EndBlock) : null;
    }

    public async Task SetBlockIndexAsync(long blockIndex)
    {
        await _redis.StringSetAsync(
            $"{PREFIX}:{BlockIndexKey}",
            blockIndex.ToString(),
            TimeSpan.FromMinutes(10)
        );
    }

    public async Task SetSeasonAsync(int seasonId, long startBlock, long endBlock)
    {
        var seasonData = new CachedSeason
        {
            Id = seasonId,
            StartBlock = startBlock,
            EndBlock = endBlock
        };

        var json = JsonSerializer.Serialize(seasonData);

        await _redis.StringSetAsync($"{PREFIX}:{SeasonKey}", json, TimeSpan.FromDays(31));
    }

    public async Task SetRoundAsync(int roundId, long startBlock, long endBlock)
    {
        var roundData = new CachedRound
        {
            Id = roundId,
            StartBlock = startBlock,
            EndBlock = endBlock
        };

        var json = JsonSerializer.Serialize(roundData);

        await _redis.StringSetAsync($"{PREFIX}:{RoundKey}", json, TimeSpan.FromHours(12));
    }

    private class CachedSeason
    {
        public int Id { get; set; }
        public long StartBlock { get; set; }
        public long EndBlock { get; set; }
    }

    private class CachedRound
    {
        public int Id { get; set; }
        public long StartBlock { get; set; }
        public long EndBlock { get; set; }
    }
}

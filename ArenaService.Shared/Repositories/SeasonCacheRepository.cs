using System.Text.Json;
using ArenaService.Shared.Exceptions;
using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface ISeasonCacheRepository
{
    Task<long> GetBlockIndexAsync();
    Task<(int Id, long StartBlock, long EndBlock)> GetSeasonAsync();
    Task<(int Id, int RoundIndex, long StartBlock, long EndBlock)> GetRoundAsync();
    Task SetBlockIndexAsync(long blockIndex);
    Task SetSeasonAsync(int seasonId, long startBlock, long endBlock);
    Task SetRoundAsync(int roundId, int roundIndex, long startBlock, long endBlock);
    Task DeleteBlockIndexAsync();
    Task DeleteSeasonAsync();
    Task DeleteRoundAsync();
    Task DeleteAllAsync();
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

    public async Task<long> GetBlockIndexAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{BlockIndexKey}");

        if (!value.HasValue)
        {
            throw new CacheUnavailableException("Cache is unavailable.");
        }

        return long.Parse(value!);
    }

    public async Task<(int Id, long StartBlock, long EndBlock)> GetSeasonAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{SeasonKey}");

        if (!value.HasValue)
        {
            throw new CacheUnavailableException("Season cache is unavailable.");
        }

        var seasonData = JsonSerializer.Deserialize<CachedSeason>(value!);
        return (seasonData!.Id, seasonData.StartBlock, seasonData.EndBlock);
    }

    public async Task<(int Id, int RoundIndex, long StartBlock, long EndBlock)> GetRoundAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{RoundKey}");

        if (!value.HasValue)
        {
            throw new CacheUnavailableException("Round cache is unavailable.");
        }

        var roundData = JsonSerializer.Deserialize<CachedRound>(value!);
        return (roundData!.Id, roundData.RoundIndex, roundData.StartBlock, roundData.EndBlock);
    }

    public async Task SetBlockIndexAsync(long blockIndex)
    {
        await _redis.StringSetAsync($"{PREFIX}:{BlockIndexKey}", blockIndex.ToString());
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

        await _redis.StringSetAsync($"{PREFIX}:{SeasonKey}", json);
    }

    public async Task SetRoundAsync(int roundId, int roundIndex, long startBlock, long endBlock)
    {
        var roundData = new CachedRound
        {
            Id = roundId,
            RoundIndex = roundIndex,
            StartBlock = startBlock,
            EndBlock = endBlock
        };

        var json = JsonSerializer.Serialize(roundData);

        await _redis.StringSetAsync($"{PREFIX}:{RoundKey}", json);
    }

    public async Task DeleteBlockIndexAsync()
    {
        await _redis.KeyDeleteAsync($"{PREFIX}:{BlockIndexKey}");
    }

    public async Task DeleteSeasonAsync()
    {
        await _redis.KeyDeleteAsync($"{PREFIX}:{SeasonKey}");
    }

    public async Task DeleteRoundAsync()
    {
        await _redis.KeyDeleteAsync($"{PREFIX}:{RoundKey}");
    }

    public async Task DeleteAllAsync()
    {
        await _redis.KeyDeleteAsync($"{PREFIX}:{BlockIndexKey}");
        await _redis.KeyDeleteAsync($"{PREFIX}:{SeasonKey}");
        await _redis.KeyDeleteAsync($"{PREFIX}:{RoundKey}");
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
        public int RoundIndex { get; set; }
        public long StartBlock { get; set; }
        public long EndBlock { get; set; }
    }
}

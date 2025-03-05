using StackExchange.Redis;

namespace ArenaService.Shared.Repositories;

public interface IBlockTrackerRepository
{
    Task<long> GetBattleTxTrackerBlockIndexAsync();
    Task SetBattleTxTrackerBlockIndexAsync(long blockIndex);
}

public class BlockTrackerRepository : IBlockTrackerRepository
{
    private readonly IDatabase _redis;
    private const string PREFIX = "block_tracker";
    private const string BATTLE_TX_TRACKER_KEY = "battle_tx_tracker:last_processed_block";

    public BlockTrackerRepository(IConnectionMultiplexer redis)
    {
        _redis = redis.GetDatabase();
    }

    public async Task<long> GetBattleTxTrackerBlockIndexAsync()
    {
        var value = await _redis.StringGetAsync($"{PREFIX}:{BATTLE_TX_TRACKER_KEY}");
        return value.HasValue ? (long)value : -1;
    }

    public async Task SetBattleTxTrackerBlockIndexAsync(long blockIndex)
    {
        await _redis.StringSetAsync($"{PREFIX}:{BATTLE_TX_TRACKER_KEY}", blockIndex);
    }
} 
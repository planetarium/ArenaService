using ArenaService.Client;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Repositories;
using ArenaService.Utils;
using Bencodex;
using Hangfire;
using Libplanet.Types.Tx;
using StackExchange.Redis;

namespace ArenaService.Worker;

public class BattleTxTracker : BackgroundService
{
    private readonly ILogger<BattleTxTracker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const string LAST_PROCESSED_BLOCK_KEY = "battle_tx_tracker:last_processed_block";
    private const string ACTION_TYPE = "battle";
    private static readonly Codec Codec = new();

    public BattleTxTracker(ILogger<BattleTxTracker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting BattleTxTracker...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var client = scope.ServiceProvider.GetRequiredService<IHeadlessClient>();
                    var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();
                    var jobClient =
                        scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
                    var battleRepo = scope.ServiceProvider.GetRequiredService<IBattleRepository>();
                    var battleTokenValidator =
                        scope.ServiceProvider.GetRequiredService<BattleTokenValidator>();

                    await ProcessTransactionsAsync(
                        client,
                        redis.GetDatabase(),
                        seasonCacheRepo,
                        battleRepo,
                        jobClient,
                        battleTokenValidator,
                        stoppingToken
                    );
                }
            }
            catch (TaskCanceledException ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "HTTP request timed out. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"An error occurred in Headless.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, $"An error occurred in Redis.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in BattleTxTracker");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessTransactionsAsync(
        IHeadlessClient client,
        IDatabase redis,
        ISeasonCacheRepository seasonCacheRepo,
        IBattleRepository battleRepo,
        IBackgroundJobClient jobClient,
        BattleTokenValidator battleTokenValidator,
        CancellationToken stoppingToken
    )
    {
        var currentBlockIndex = await seasonCacheRepo.GetBlockIndexAsync();
        var lastProcessedBlock = await GetLastProcessedBlockAsync(redis);
        var startingBlock = lastProcessedBlock;

        if (startingBlock > currentBlockIndex)
        {
            return;
        }

        var blockDiff = currentBlockIndex - startingBlock;
        var limit = blockDiff switch
        {
            > 1000 => 300,
            > 500 => 100,
            > 100 => 30,
            > 50 => 10,
            > 30 => 5,
            _ => 1
        };

        _logger.LogInformation(
            $"Processing transactions from block {startingBlock} to {currentBlockIndex}"
        );

        var response = await RetryUtility.RetryAsync(
            async () =>
            {
                var response = await client.GetTxs.ExecuteAsync(
                    startingBlock,
                    limit,
                    ACTION_TYPE,
                    [TxStatus.Success, TxStatus.Staging],
                    stoppingToken
                );
                if (response?.Data?.Transaction?.NcTransactions == null)
                {
                    _logger.LogInformation("Transaction response is null. Retrying...");
                    return null;
                }
                return response;
            },
            maxAttempts: 5,
            delayMilliseconds: 2000,
            successCondition: response => response?.Data?.Transaction?.NcTransactions != null,
            onRetry: attempt =>
            {
                _logger.LogDebug(
                    $"Retry attempt {attempt}: Transaction response is null, retrying..."
                );
            }
        );

        if (response?.Data?.Transaction?.NcTransactions == null)
        {
            return;
        }

        foreach (var tx in response.Data.Transaction.NcTransactions)
        {
            try
            {
                foreach (var action in tx!.Actions)
                {
                    var actionValue = Codec.Decode(Convert.FromHexString(action!.Raw));

                    if (
                        BattleActionParser.TryParseActionPayload(
                            actionValue,
                            out var battleActionValue
                        )
                    )
                    {
                        _logger.LogInformation($"Found battle action in transaction {tx.Id}");
                        var battleToken = battleActionValue.Memo;

                        if (
                            !battleTokenValidator.TryValidateBattleToken(
                                battleToken,
                                out var payload
                            )
                        )
                        {
                            continue;
                        }

                        var battle = await battleRepo.UpdateBattle(
                            Convert.ToInt32(payload["bid"]),
                            b =>
                            {
                                b.TxId = TxId.FromHex(tx.Id);
                            }
                            );

                        jobClient.Enqueue<BattleProcessor>(x =>
                            x.ProcessAsync(Convert.ToInt32(payload["bid"]))
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process transaction {tx.Id}");
                throw;
            }
        }

        await SetLastProcessedBlockAsync(redis, startingBlock + limit - 1);
    }

    private async Task<long> GetLastProcessedBlockAsync(IDatabase redis)
    {
        var value = await redis.StringGetAsync(LAST_PROCESSED_BLOCK_KEY);
        return value.HasValue ? (long)value : -1;
    }

    private async Task SetLastProcessedBlockAsync(IDatabase redis, long blockIndex)
    {
        await redis.StringSetAsync(LAST_PROCESSED_BLOCK_KEY, blockIndex);
    }
}

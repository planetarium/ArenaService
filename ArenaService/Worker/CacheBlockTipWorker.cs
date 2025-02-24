using ArenaService.Client;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ArenaService.Worker;

public class CacheBlockTipWorker : BackgroundService
{
    private readonly ILogger<CacheBlockTipWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CacheBlockTipWorker(
        ILogger<CacheBlockTipWorker> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start BlockIndexCachingWorker");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var client = scope.ServiceProvider.GetRequiredService<IHeadlessClient>();
                    var seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();

                    await ProcessAsync(client, seasonService, seasonCacheRepo, stoppingToken);
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
            catch (NotFoundSeasonException ex)
            {
                _logger.LogError(ex, $"Not found season, plz insert seasons");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred in {nameof(CacheBlockTipWorker)}.");
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        IHeadlessClient client,
        ISeasonService seasonService,
        ISeasonCacheRepository seasonCacheRepo,
        CancellationToken stoppingToken
    )
    {
        var blockIndex = await GetCurrentBlockIndexAsync(client, stoppingToken);
        if (blockIndex == null)
        {
            _logger.LogWarning("Failed to fetch current block index.");
            return;
        }

        await seasonCacheRepo.SetBlockIndexAsync(blockIndex.Value);

        _logger.LogInformation($"Block index: {blockIndex}");

        await HandleSeason(blockIndex.Value, seasonService, seasonCacheRepo);
        await HandleRound(blockIndex.Value, seasonService, seasonCacheRepo);
    }

    private async Task<long?> GetCurrentBlockIndexAsync(
        IHeadlessClient client,
        CancellationToken stoppingToken
    )
    {
        var tipResponse = await client.GetTipIndex.ExecuteAsync(stoppingToken);
        return tipResponse.Data?.NodeStatus.Tip.Index;
    }

    private async Task HandleSeason(
        long blockIndex,
        ISeasonService seasonService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        bool shouldUpdate;
        try
        {
            var cachedSeason = await seasonCacheRepo.GetSeasonAsync();
            shouldUpdate =
                blockIndex < cachedSeason.StartBlock || blockIndex > cachedSeason.EndBlock;
        }
        catch (CacheUnavailableException)
        {
            shouldUpdate = true;
        }

        if (shouldUpdate)
        {
            var seasonInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex);

            await seasonCacheRepo.SetSeasonAsync(
                seasonInfo.Season.Id,
                seasonInfo.Season.StartBlock,
                seasonInfo.Season.EndBlock
            );
        }
    }

    private async Task HandleRound(
        long blockIndex,
        ISeasonService seasonService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        bool shouldUpdate;

        try
        {
            var cachedRound = await seasonCacheRepo.GetRoundAsync();
            shouldUpdate = blockIndex < cachedRound.StartBlock || blockIndex > cachedRound.EndBlock;
        }
        catch (CacheUnavailableException)
        {
            shouldUpdate = true;
        }

        if (shouldUpdate)
        {
            var seasonInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex);

            await seasonCacheRepo.SetRoundAsync(
                seasonInfo.Round.Id,
                seasonInfo.Round.StartBlock,
                seasonInfo.Round.EndBlock
            );
        }
    }
}

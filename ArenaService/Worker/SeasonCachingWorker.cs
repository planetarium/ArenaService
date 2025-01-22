using ArenaService.Client;
using ArenaService.Exceptions;
using ArenaService.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ArenaService.Worker;

public class SeasonCachingWorker : BackgroundService
{
    private readonly ILogger<SeasonCachingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SeasonCachingWorker(
        ILogger<SeasonCachingWorker> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting BlockIndexCachingWorker...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var client = scope.ServiceProvider.GetRequiredService<IHeadlessClient>();
                    var seasonRepo = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
                    var rankingRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingRepository>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();

                    await ProcessBlockIndexAsync(
                        client,
                        seasonRepo,
                        seasonCacheRepo,
                        rankingRepo,
                        stoppingToken
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in BlockIndexCachingWorker.");
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBlockIndexAsync(
        IHeadlessClient client,
        ISeasonRepository seasonRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IRankingRepository rankingRepo,
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

        if (await IsBlockWithinCachedRoundAsync(blockIndex.Value, seasonCacheRepo))
        {
            _logger.LogInformation(
                "Block index is within the cached season and round. No updates needed."
            );
            return;
        }

        await UpdateSeasonAndRoundCacheAsync(
            blockIndex.Value,
            seasonRepo,
            seasonCacheRepo,
            rankingRepo
        );
    }

    private async Task<long?> GetCurrentBlockIndexAsync(
        IHeadlessClient client,
        CancellationToken stoppingToken
    )
    {
        var tipResponse = await client.GetTipIndex.ExecuteAsync(stoppingToken);
        return tipResponse.Data?.NodeStatus.Tip.Index;
    }

    private async Task<bool> IsBlockWithinCachedRoundAsync(
        long blockIndex,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        try
        {
            var cachedSeason = await seasonCacheRepo.GetSeasonAsync();
            var cachedRound = await seasonCacheRepo.GetRoundAsync();

            if (blockIndex < cachedSeason.StartBlock || blockIndex > cachedSeason.EndBlock)
            {
                return false;
            }

            if (blockIndex < cachedRound.StartBlock || blockIndex > cachedRound.EndBlock)
            {
                return false;
            }
        }
        catch (CacheUnavailableException)
        {
            return false;
        }

        return true;
    }

    private async Task UpdateSeasonAndRoundCacheAsync(
        long blockIndex,
        ISeasonRepository seasonRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IRankingRepository rankingRepo
    )
    {
        var seasons = await seasonRepo.GetAllSeasonsAsync();
        var currentSeason = seasons.FirstOrDefault(s =>
            s.StartBlock <= blockIndex && s.EndBlock >= blockIndex
        );

        if (currentSeason == null)
        {
            _logger.LogWarning("No matching season found for the current block index.");
            return;
        }

        var currentRound = currentSeason.Rounds.FirstOrDefault(ai =>
            ai.StartBlock <= blockIndex && ai.EndBlock >= blockIndex
        );

        if (currentRound == null)
        {
            _logger.LogWarning("No matching round found for the current block index.");
            return;
        }

        await seasonCacheRepo.SetSeasonAsync(
            currentSeason.Id,
            currentSeason.StartBlock,
            currentSeason.EndBlock
        );
        await seasonCacheRepo.SetRoundAsync(
            currentRound.Id,
            currentRound.StartBlock,
            currentRound.EndBlock
        );
        await rankingRepo.CopyRoundDataAsync(
            currentSeason.Id,
            currentRound.Id,
            currentRound.Id + 1
        );

        _logger.LogInformation(
            "Updated cache: BlockIndex={BlockIndex}, SeasonId={SeasonId}, RoundId={RoundId}",
            blockIndex,
            currentSeason.Id,
            currentRound.Id
        );
    }
}

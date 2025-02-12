using ArenaService.Exceptions;
using ArenaService.Repositories;

namespace ArenaService.Worker;

public class AllClanRankingWorker : BackgroundService
{
    private readonly ILogger<AllClanRankingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const int UpdateIntervalSeconds = 10;

    public AllClanRankingWorker(
        ILogger<AllClanRankingWorker> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AllClanRankingWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var rankingService =
                        scope.ServiceProvider.GetRequiredService<IRankingService>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();

                    await ProcessAsync(rankingService, seasonCacheRepo);
                }
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogError(ex, $"Not cached season, round");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in AllClanRankingWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(UpdateIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        IRankingService rankingService,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        try
        {
            var cachedSeason = await seasonCacheRepo.GetSeasonAsync();
            var cachedRound = await seasonCacheRepo.GetRoundAsync();

            _logger.LogInformation(
                $"Updating all clan ranking for Season {cachedSeason.Id}, Round {cachedRound.Id}"
            );

            await rankingService.UpdateAllClanRankingAsync(
                cachedSeason.Id,
                cachedRound.Id,
                (int)(cachedRound.EndBlock - cachedRound.StartBlock)
            );

            _logger.LogInformation("All clan ranking update completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update all clan rankings.");
        }
    }
}

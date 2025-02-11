using ArenaService.Services;
using ArenaService.Constants;
using ArenaService.Exceptions;
using ArenaService.Models;
using ArenaService.Repositories;
using StackExchange.Redis;

namespace ArenaService.Worker;

public class RankingCopyWorker : BackgroundService
{
    private readonly ILogger<CacheBlockTipWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private bool prepareInProgress = false;

    public RankingCopyWorker(ILogger<CacheBlockTipWorker> logger, IServiceProvider serviceProvider)
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
                    var seasonRepo = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
                    var rankingSnapshotRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingSnapshotRepository>();
                    var rankingService =
                        scope.ServiceProvider.GetRequiredService<IRankingService>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();
                    var rankingRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingRepository>();
                    var clanRankingRepo =
                        scope.ServiceProvider.GetRequiredService<IClanRankingRepository>();
                    var seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

                    var cachedBlockIndex = await seasonCacheRepo.GetBlockIndexAsync();
                    var cachedRound = await seasonCacheRepo.GetRoundAsync();
                    var cachedSeason = await seasonCacheRepo.GetSeasonAsync();

                    // 라운드가 끝나기 5 블록 전에 다음 라운드 랭킹을 준비합니다.
                    if (cachedBlockIndex >= cachedRound.EndBlock - 5)
                    {
                        await ProcessAsync(
                            cachedBlockIndex,
                            rankingSnapshotRepo,
                            rankingRepo,
                            clanRankingRepo,
                            seasonService
                        );
                        await Task.Delay(
                            TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS),
                            stoppingToken
                        );
                    }
                }
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, $"An error occurred in Redis.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (CacheUnavailableException ex)
            {
                _logger.LogError(ex, $"Not cached season, round");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred in {nameof(RankingCopyWorker)}.");
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        long blockIndex,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        ISeasonService seasonService
    )
    {
        var nextRoundInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex + 6);

        if (
            !prepareInProgress
            && await rankingSnapshotRepo.GetRankingSnapshotsCount(
                nextRoundInfo.Season.Id,
                nextRoundInfo.Round.Id
            ) <= 0
        )
        {
            await PrepareNextRound(
                nextRoundInfo,
                rankingSnapshotRepo,
                rankingRepo,
                clanRankingRepo
            );
        }
        else
        {
            _logger.LogInformation($"Round {nextRoundInfo.Round.Id}: Already prepared round.");
        }
    }

    private async Task PrepareNextRound(
        (Season Season, Round Round) nextRoundInfo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo
    )
    {
        prepareInProgress = true;
        _logger.LogInformation($"Start PrepareNextRound {nextRoundInfo.Round.Id}");

        // 다음 라운드의 +1 한 라운드를 준비합니다.
        await rankingRepo.CopyRoundDataAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id,
            nextRoundInfo.Round.Id + 1,
            nextRoundInfo.Season.RoundInterval
        );
        await clanRankingRepo.CopyRoundDataAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id,
            nextRoundInfo.Round.Id + 1,
            nextRoundInfo.Season.RoundInterval
        );

        // 다음 라운드의 랭킹 정보를 저장해둡니다.
        var rankingData = await rankingRepo.GetScoresAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );
        var clanRankingData = await clanRankingRepo.GetScoresAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );

        await rankingSnapshotRepo.AddRankingsSnapshot(
            rankingData,
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );
        await rankingSnapshotRepo.AddClanRankingsSnapshot(
            clanRankingData,
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );

        prepareInProgress = false;
        _logger.LogInformation($"PrepareNextRound {nextRoundInfo.Round.Id} Done");
    }
}

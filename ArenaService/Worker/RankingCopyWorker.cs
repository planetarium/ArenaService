using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
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
        _logger.LogInformation("Starting RankingCopyWorker...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var seasonRepo = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
                    var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
                    var rankingSnapshotRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingSnapshotRepository>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();
                    var roundPreparationService =
                        scope.ServiceProvider.GetRequiredService<IRoundPreparationService>();
                    var seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

                    var cachedBlockIndex = await seasonCacheRepo.GetBlockIndexAsync();
                    var cachedRound = await seasonCacheRepo.GetRoundAsync();
                    var cachedSeason = await seasonCacheRepo.GetSeasonAsync();

                    _logger.LogInformation(
                        $"Check prepare next round {cachedBlockIndex >= cachedRound.EndBlock - 9}"
                    );
                    // 라운드가 끝나기 9 블록 전에 다음 라운드 랭킹을 준비합니다.
                    if (cachedBlockIndex >= cachedRound.EndBlock - 9)
                    {
                        await ProcessAsync(
                            cachedBlockIndex,
                            rankingSnapshotRepo,
                            seasonService,
                            roundPreparationService
                        );
                        await Task.Delay(
                            TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS),
                            stoppingToken
                        );
                    }
                    // 만약 현재 시즌에 대한 스냅샷이 없다면 copy 진행
                    else if (
                        !prepareInProgress
                        & await rankingSnapshotRepo.GetRankingSnapshotsCount(
                            cachedSeason.Id,
                            cachedRound.Id
                        ) <= 0
                    )
                    {
                        var season = await seasonRepo.GetSeasonAsync(
                            cachedSeason.Id,
                            q => q.Include(s => s.Rounds)
                        );
                        var round = await roundRepo.GetRoundAsync(cachedRound.Id);

                        await PrepareNextRound((season!, round!), roundPreparationService);

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

            await Task.Delay(
                TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS),
                stoppingToken
            );
        }
    }

    private async Task ProcessAsync(
        long blockIndex,
        IRankingSnapshotRepository rankingSnapshotRepo,
        ISeasonService seasonService,
        IRoundPreparationService roundPreparationService
    )
    {
        var nextRoundInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex + 10);

        if (
            !prepareInProgress
            & await rankingSnapshotRepo.GetRankingSnapshotsCount(
                nextRoundInfo.Season.Id,
                nextRoundInfo.Round.Id
            ) <= 0
        )
        {
            await PrepareNextRound(nextRoundInfo, roundPreparationService);
        }
        else
        {
            _logger.LogInformation($"Round {nextRoundInfo.Round.Id}: Already prepared round.");
        }
    }

    private async Task PrepareNextRound(
        (Season Season, Round Round) nextRoundInfo,
        IRoundPreparationService roundPreparationService
    )
    {
        prepareInProgress = true;

        await roundPreparationService.PrepareNextRoundWithSnapshotAsync(nextRoundInfo);

        prepareInProgress = false;
        _logger.LogInformation($"PrepareNextRound {nextRoundInfo.Round.Id} Done");
    }
}

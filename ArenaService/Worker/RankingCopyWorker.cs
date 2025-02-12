using ArenaService.Constants;
using ArenaService.Exceptions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
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
                    var rankingSnapshotRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingSnapshotRepository>();
                    var rankingService =
                        scope.ServiceProvider.GetRequiredService<IRankingService>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();
                    var rankingRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingRepository>();
                    var clanRepo = scope.ServiceProvider.GetRequiredService<IClanRepository>();
                    var clanRankingRepo =
                        scope.ServiceProvider.GetRequiredService<IClanRankingRepository>();
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
                            rankingRepo,
                            clanRankingRepo,
                            seasonService,
                            clanRepo,
                            rankingService
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

            await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        long blockIndex,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        ISeasonService seasonService,
        IClanRepository clanRepo,
        IRankingService rankingService
    )
    {
        var nextRoundInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex + 10);

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
                clanRankingRepo,
                clanRepo,
                rankingService
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
        IClanRankingRepository clanRankingRepo,
        IClanRepository clanRepo,
        IRankingService rankingService
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

        var clanIds = await clanRankingRepo.GetClansAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );
        var rankingData = await rankingRepo.GetScoresAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );
        var clans = new Dictionary<Address, int>();

        foreach (var clanId in clanIds)
        {
            await clanRankingRepo.CopyRoundDataAsync(
                clanId,
                nextRoundInfo.Season.Id,
                nextRoundInfo.Round.Id,
                nextRoundInfo.Round.Id + 1,
                nextRoundInfo.Season.RoundInterval
            );
            var clan = await clanRepo.GetClan(clanId, q => q.Include(c => c.Users));
            if (clan != null)
            {
                foreach (var user in clan.Users)
                {
                    clans.Add(user.AvatarAddress, clanId);
                }
            }
        }

        var rankingDataWithClan = new List<(Address AvatarAddress, int? ClanId, int Score)>();
        foreach (var rankingEntry in rankingData)
        {
            rankingDataWithClan.Add(
                (
                    rankingEntry.AvatarAddress,
                    clans.TryGetValue(rankingEntry.AvatarAddress, out var value) ? value : null,
                    rankingEntry.Score
                )
            );
        }

        // 다음 라운드의 랭킹 정보를 저장해둡니다.
        await rankingSnapshotRepo.AddRankingsSnapshot(
            rankingDataWithClan,
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id
        );

        await rankingService.UpdateAllClanRankingAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id + 1,
            nextRoundInfo.Season.RoundInterval
        );

        prepareInProgress = false;
        _logger.LogInformation($"PrepareNextRound {nextRoundInfo.Round.Id} Done");
    }
}

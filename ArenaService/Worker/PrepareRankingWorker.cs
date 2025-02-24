using System.Text.Json;
using ArenaService.Client;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ArenaService.Worker;

public class PrepareRankingWorker : BackgroundService
{
    private const int batchSize = 300;
    private readonly ILogger<CacheBlockTipWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool prepareInProgress = false;

    public PrepareRankingWorker(
        ILogger<CacheBlockTipWorker> logger,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start PrepareRankingWorker...");

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
                    var rankingService =
                        scope.ServiceProvider.GetRequiredService<IRankingService>();
                    var seasonCacheRepo =
                        scope.ServiceProvider.GetRequiredService<ISeasonCacheRepository>();
                    var participantRepo =
                        scope.ServiceProvider.GetRequiredService<IParticipantRepository>();
                    var rankingRepo =
                        scope.ServiceProvider.GetRequiredService<IRankingRepository>();
                    var clanRankingRepo =
                        scope.ServiceProvider.GetRequiredService<IClanRankingRepository>();
                    var seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();
                    var seasonPreparationService =
                        scope.ServiceProvider.GetRequiredService<ISeasonPreparationService>();

                    var cachedBlockIndex = await seasonCacheRepo.GetBlockIndexAsync();
                    var cachedRound = await seasonCacheRepo.GetRoundAsync();
                    var cachedSeason = await seasonCacheRepo.GetSeasonAsync();

                    _logger.LogInformation(
                        $"Check prepare next season {cachedBlockIndex >= cachedSeason.EndBlock - 50}"
                    );

                    // 시즌이 끝나기 50블록 전부터 다음 시즌에 대한 정보를 미리 주입해둡니다.
                    if (cachedBlockIndex >= cachedSeason.EndBlock - 50)
                    {
                        await ProcessAsync(
                            cachedBlockIndex,
                            seasonService,
                            rankingSnapshotRepo,
                            seasonPreparationService
                        );
                        await Task.Delay(
                            TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * 5),
                            stoppingToken
                        );
                    }
                    // 만약 현재 시즌에 대한 참가자가 없다면 진행
                    else if (
                        !prepareInProgress
                        & await participantRepo.GetParticipantCountAsync(cachedSeason.Id) <= 0
                    )
                    {
                        var season = await seasonRepo.GetSeasonAsync(
                            cachedSeason.Id,
                            q => q.Include(s => s.Rounds)
                        );
                        var firstRound = season.Rounds.OrderBy(r => r.StartBlock).First();

                        await PrepareNextSeason((season!, firstRound!), seasonPreparationService);

                        await Task.Delay(
                            TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS),
                            stoppingToken
                        );
                    }

                    // 캐시를 복구해야하는지 판단합니다.
                    if (await rankingRepo.GetRankingStatus(cachedSeason.Id, cachedRound.Id) is null)
                    {
                        await RestoreRankings(
                            cachedBlockIndex,
                            participantRepo,
                            seasonService,
                            rankingSnapshotRepo,
                            rankingRepo,
                            clanRankingRepo,
                            rankingService
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
                _logger.LogError(ex, $"An error occurred in {nameof(PrepareRankingWorker)}.");
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
        ISeasonService seasonService,
        IRankingSnapshotRepository rankingSnapshotRepo,
        ISeasonPreparationService seasonPreparationService
    )
    {
        var nextSeason = await seasonService.GetSeasonAndRoundByBlock(blockIndex + 51);

        // 캐싱 중이거나 캐싱이 된 상태라면 진행하지 않습니다.
        if (
            !prepareInProgress
            & await rankingSnapshotRepo.GetRankingSnapshotsCount(
                nextSeason.Season.Id,
                nextSeason.Round.Id
            ) <= 0
        )
        {
            await PrepareNextSeason(nextSeason, seasonPreparationService);
        }
        else
        {
            _logger.LogInformation($"Season {nextSeason.Season.Id}: Already prepared season.");
        }
    }

    private async Task PrepareNextSeason(
        (Season Season, Round Round) nextSeason,
        ISeasonPreparationService seasonPreparationService
    )
    {
        prepareInProgress = true;
        _logger.LogInformation($"Start PrepareNextSeason {nextSeason.Season.Id}");

        await seasonPreparationService.PrepareSeasonAsync(nextSeason);

        prepareInProgress = false;
        _logger.LogInformation($"PrepareNextSeason {nextSeason.Season.Id} Done");
    }

    private async Task RestoreRankings(
        long blockIndex,
        IParticipantRepository participantRepo,
        ISeasonService seasonService,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IRankingService rankingService
    )
    {
        // 지금 진행되고 있는 시즌 정보를 받아옵니다.
        var seasonInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex);
        _logger.LogInformation($"Restore cache {seasonInfo.Season.Id} {seasonInfo.Round.Id}");

        // 스냅샷에서 복구해 현재 라운드의 랭킹을 캐시합니다.
        var rankingSnapshots = await rankingSnapshotRepo.GetRankingSnapshots(
            seasonInfo.Season.Id,
            seasonInfo.Round.Id
        );

        var rankingData = rankingSnapshots
            .Select(r => (r.AvatarAddress, r.ClanId, r.Score))
            .ToList();

        var clanRankingsData = new Dictionary<int, List<(Address, int)>>();
        foreach (var (avatarAddress, clanId, score) in rankingData)
        {
            if (clanId is not null)
            {
                try
                {
                    clanRankingsData[clanId.Value].Add((avatarAddress, score));
                }
                catch (KeyNotFoundException)
                {
                    clanRankingsData[clanId.Value] = new List<(Address, int)>()
                    {
                        (avatarAddress, score)
                    };
                }
            }
        }

        await rankingRepo.InitRankingAsync(
            rankingData.Select(r => (r.AvatarAddress, r.Score)).ToList(),
            seasonInfo.Season.Id,
            seasonInfo.Round.Id,
            seasonInfo.Season.RoundInterval
        );
        foreach (var (clanId, clanRankingData) in clanRankingsData)
        {
            await clanRankingRepo.InitRankingAsync(
                clanRankingData,
                clanId,
                seasonInfo.Season.Id,
                seasonInfo.Round.Id,
                seasonInfo.Season.RoundInterval
            );
        }

        int skip = 0;
        while (true)
        {
            // 다음 라운드의 대한 정보는 현재 유저들의 score로 초기화합니다.
            var participants = await participantRepo.GetParticipantsAsync(
                seasonInfo.Season.Id,
                skip,
                batchSize,
                q => q.Include(p => p.User)
            );

            if (!participants.Any())
                break;

            _logger.LogInformation($"Restore rankings {participants.Count}");

            var nextRoundRankingData = participants
                .Select(r => (r.AvatarAddress, r.Score))
                .ToList();
            var nextRoundClanRankingsData = new Dictionary<int, List<(Address, int)>>();
            foreach (var participant in participants)
            {
                if (participant.User.ClanId is not null)
                {
                    try
                    {
                        nextRoundClanRankingsData[participant.User.ClanId.Value]
                            .Add((participant.AvatarAddress, participant.Score));
                    }
                    catch (KeyNotFoundException)
                    {
                        nextRoundClanRankingsData[participant.User.ClanId.Value] = new List<(
                            Address,
                            int
                        )>()
                        {
                            (participant.AvatarAddress, participant.Score)
                        };
                    }
                }
            }

            await rankingRepo.InitRankingAsync(
                nextRoundRankingData,
                seasonInfo.Season.Id,
                seasonInfo.Round.Id + 1,
                seasonInfo.Season.RoundInterval
            );

            foreach (var (clanId, nextRoundClanRankingData) in nextRoundClanRankingsData)
            {
                await clanRankingRepo.InitRankingAsync(
                    nextRoundClanRankingData,
                    clanId,
                    seasonInfo.Season.Id,
                    seasonInfo.Round.Id + 1,
                    seasonInfo.Season.RoundInterval
                );
            }

            skip += batchSize;
        }
        await rankingService.UpdateAllClanRankingAsync(
            seasonInfo.Season.Id,
            seasonInfo.Round.Id,
            seasonInfo.Season.RoundInterval
        );
        await rankingService.UpdateAllClanRankingAsync(
            seasonInfo.Season.Id,
            seasonInfo.Round.Id + 1,
            seasonInfo.Season.RoundInterval
        );

        _logger.LogInformation(
            $"Restore ranking {seasonInfo.Season.Id} {seasonInfo.Round.Id} Done"
        );
    }
}

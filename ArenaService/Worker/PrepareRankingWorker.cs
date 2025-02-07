using ArenaService.Client;
using ArenaService.Services;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
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
                    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    var seasonRepo = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
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
                    var medalRepo = scope.ServiceProvider.GetRequiredService<IMedalRepository>();
                    var seasonService = scope.ServiceProvider.GetRequiredService<ISeasonService>();

                    var cachedBlockIndex = await seasonCacheRepo.GetBlockIndexAsync();
                    var cachedRound = await seasonCacheRepo.GetRoundAsync();
                    var cachedSeason = await seasonCacheRepo.GetSeasonAsync();

                    // 시즌이 끝나기 50블록 전부터 다음 시즌에 대한 정보를 미리 주입해둡니다.
                    if (cachedBlockIndex >= cachedSeason.EndBlock - 50)
                    {
                        await ProcessAsync(
                            cachedBlockIndex,
                            participantRepo,
                            seasonService,
                            rankingSnapshotRepo,
                            rankingRepo,
                            clanRankingRepo,
                            medalRepo
                        );
                        await Task.Delay(
                            TimeSpan.FromSeconds(ArenaServiceConfig.BLOCK_INTERVAL_SECONDS * 5),
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
                            clanRankingRepo
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

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        long blockIndex,
        IParticipantRepository participantRepo,
        ISeasonService seasonService,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IMedalRepository medalRepo
    )
    {
        var nextSeason = await seasonService.GetSeasonAndRoundByBlock(blockIndex + 51);

        // 캐싱 중이거나 캐싱이 된 상태라면 진행하지 않습니다.
        if (
            !prepareInProgress
            && await rankingSnapshotRepo.GetRankingSnapshotsCount(
                nextSeason.Season.Id,
                nextSeason.Round.Id
            ) <= 0
        )
        {
            await PrepareNextSeason(
                nextSeason,
                participantRepo,
                rankingSnapshotRepo,
                rankingRepo,
                clanRankingRepo,
                medalRepo,
                seasonService
            );
        }
        else
        {
            _logger.LogInformation($"Season {nextSeason.Season.Id}: Already prepared season.");
        }
    }

    private async Task PrepareNextSeason(
        (Season Season, Round Round) nextSeason,
        IParticipantRepository participantRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IMedalRepository medalRepo,
        ISeasonService seasonService
    )
    {
        prepareInProgress = true;
        _logger.LogInformation($"Start PrepareNextSeason {nextSeason.Season.Id}");

        var prevSeasonId = nextSeason.Season.Id - 1;
        Dictionary<Address, int>? medalCounts = null;
        int skip = 0;

        while (true)
        {
            // 전시즌 참가들을 300명씩 불러옵니다.
            var prevSeasonParticipants = await participantRepo.GetParticipantsAsync(
                prevSeasonId,
                skip,
                batchSize,
                q => q.Include(p => p.User)
            );

            if (!prevSeasonParticipants.Any())
                break;

            _logger.LogInformation($"Init participants and ranking {prevSeasonParticipants.Count}");

            List<Participant> eligibleParticipants;
            if (nextSeason.Season.ArenaType == ArenaType.CHAMPIONSHIP)
            {
                // 챔피언쉽이면 메달 개수를 충족했는지 확인합니다.
                if (medalCounts is null)
                {
                    var seasons = await seasonService.ClassifyByChampionship(
                        nextSeason.Season.StartBlock + 1
                    );
                    var onlySeasons = seasons.Where(s => s.ArenaType == ArenaType.SEASON).ToList();
                    if (!onlySeasons.Any())
                    {
                        throw new NotFoundSeasonException("Not found seasons for check medals");
                    }

                    // 아바타 기준이 아닌 시즌별로 총합 메달 카운트가 들어있기 때문에 캐싱합니다.
                    medalCounts = await medalRepo.GetMedalsBySeasonsAsync(
                        onlySeasons.Select(s => s.Id).ToList()
                    );
                }

                // 자격이 있는 참가자만 정리합니다.
                eligibleParticipants = prevSeasonParticipants
                    .Where(p =>
                        medalCounts.TryGetValue(p.AvatarAddress, out var totalMedals)
                        && totalMedals >= nextSeason.Season.RequiredMedalCount
                    )
                    .ToList();
                _logger.LogInformation(
                    $"Filtered eligible participants {eligibleParticipants.Count}"
                );
            }
            else
            {
                eligibleParticipants = prevSeasonParticipants.ToList();
            }

            await participantRepo.AddParticipantsAsync(
                eligibleParticipants.Select(p => p.User).ToList(),
                nextSeason.Season.Id
            );

            // 초기 랭킹 데이터를 만듭니다.
            var rankingData = eligibleParticipants.Select(p => (p.AvatarAddress, 1000)).ToList();
            var clanRankingData = new Dictionary<int, int>();
            foreach (var participant in eligibleParticipants)
            {
                if (participant.User.ClanId is not null)
                {
                    try
                    {
                        clanRankingData[participant.User.ClanId.Value] += 1000;
                    }
                    catch (KeyNotFoundException)
                    {
                        clanRankingData[participant.User.ClanId.Value] = 1000;
                    }
                }
            }

            // 시즌이 시작되는 최초 라운드에 대한 snapshot을 기록하고 랭킹을 주입해줍니다.
            // 다음 다운드, 즉 최초 라운드 + 1 라운드까지 준비해야합니다.
            await rankingSnapshotRepo.AddRankingsSnapshot(
                rankingData,
                nextSeason.Season.Id,
                nextSeason.Round.Id
            );
            await rankingSnapshotRepo.AddClanRankingsSnapshot(
                clanRankingData.Select(x => (x.Key, x.Value)).ToList(),
                nextSeason.Season.Id,
                nextSeason.Round.Id
            );
            await InitializeRankings(
                rankingData,
                clanRankingData.Select(x => (x.Key, x.Value)).ToList(),
                nextSeason.Season,
                nextSeason.Round,
                rankingRepo,
                clanRankingRepo
            );

            _logger.LogInformation($"Finish Init {eligibleParticipants.Count}");

            skip += batchSize;
        }

        prepareInProgress = false;
        _logger.LogInformation($"PrepareNextSeason {nextSeason.Season.Id} Done");
    }

    private async Task InitializeRankings(
        List<(Address, int)> rankingData,
        List<(int, int)> clanRankingData,
        Season season,
        Round round,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo
    )
    {
        await rankingRepo.InitRankingAsync(rankingData, season.Id, round.Id, season.RoundInterval);
        await clanRankingRepo.InitRankingAsync(
            clanRankingData,
            season.Id,
            round.Id,
            season.RoundInterval
        );

        await rankingRepo.InitRankingAsync(
            rankingData,
            season.Id,
            round.Id + 1,
            season.RoundInterval
        );
        await clanRankingRepo.InitRankingAsync(
            clanRankingData,
            season.Id,
            round.Id + 1,
            season.RoundInterval
        );
    }

    private async Task RestoreRankings(
        long blockIndex,
        IParticipantRepository participantRepo,
        ISeasonService seasonService,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo
    )
    {
        //  지금 진행되고 있는 시즌 정보를 받아옵니다.
        var seasonInfo = await seasonService.GetSeasonAndRoundByBlock(blockIndex);
        _logger.LogInformation($"Restore cache {seasonInfo.Season.Id} {seasonInfo.Round.Id}");

        // 스냅샷에서 복구해 현재 라운드의 랭킹을 캐시합니다.
        var rankingSnapshots = await rankingSnapshotRepo.GetRankingSnapshots(
            seasonInfo.Season.Id,
            seasonInfo.Round.Id
        );
        var clanRankingSnapshots = await rankingSnapshotRepo.GetClanRankingSnapshots(
            seasonInfo.Season.Id,
            seasonInfo.Round.Id
        );

        var rankingData = rankingSnapshots.Select(r => (r.AvatarAddress, r.Score)).ToList();
        var clanRankingData = clanRankingSnapshots.Select(cr => (cr.ClanId, cr.Score)).ToList();

        await rankingRepo.InitRankingAsync(
            rankingData,
            seasonInfo.Season.Id,
            seasonInfo.Round.Id,
            seasonInfo.Season.RoundInterval
        );
        await clanRankingRepo.InitRankingAsync(
            clanRankingData,
            seasonInfo.Season.Id,
            seasonInfo.Round.Id,
            seasonInfo.Season.RoundInterval
        );

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
            var nextRoundClanRankingData = new Dictionary<int, int>();
            foreach (var participant in participants)
            {
                if (participant.User.ClanId is not null)
                {
                    try
                    {
                        nextRoundClanRankingData[participant.User.ClanId.Value] +=
                            participant.Score;
                    }
                    catch (KeyNotFoundException)
                    {
                        nextRoundClanRankingData[participant.User.ClanId.Value] = participant.Score;
                    }
                }
            }

            await rankingRepo.InitRankingAsync(
                nextRoundRankingData,
                seasonInfo.Season.Id,
                seasonInfo.Round.Id + 1,
                seasonInfo.Season.RoundInterval
            );
            await clanRankingRepo.InitRankingAsync(
                nextRoundClanRankingData.Select(x => (x.Key, x.Value)).ToList(),
                seasonInfo.Season.Id,
                seasonInfo.Round.Id + 1,
                seasonInfo.Season.RoundInterval
            );

            skip += batchSize;
        }

        _logger.LogInformation(
            $"Restore ranking {seasonInfo.Season.Id} {seasonInfo.Round.Id} Done"
        );
    }
}

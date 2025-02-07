using ArenaService.Client;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ArenaService.Worker;

public class ArenaSupervisor : BackgroundService
{
    private readonly ILogger<ArenaSupervisor> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool prepareSeasonInProgress = false;
    private bool prepareRoundInProgress = false;

    public ArenaSupervisor(ILogger<ArenaSupervisor> logger, IServiceProvider serviceProvider)
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
                    var groupRankingRepo =
                        scope.ServiceProvider.GetRequiredService<IGroupRankingRepository>();

                    await ProcessAsync(
                        client,
                        userRepo,
                        participantRepo,
                        seasonRepo,
                        rankingSnapshotRepo,
                        seasonCacheRepo,
                        rankingRepo,
                        clanRankingRepo,
                        groupRankingRepo,
                        stoppingToken
                    );
                }
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
                _logger.LogError(ex, $"An error occurred in {nameof(ArenaSupervisor)}.");
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
        }
    }

    private async Task ProcessAsync(
        IHeadlessClient client,
        IUserRepository userRepo,
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo,
        CancellationToken stoppingToken
    )
    {
        var blockIndex = await GetCurrentBlockIndexAsync(client, stoppingToken);
        if (blockIndex == null)
        {
            _logger.LogWarning("Failed to fetch current block index.");
            return;
        }

        try
        {
            await seasonCacheRepo.GetSeasonAsync();
        }
        catch (CacheUnavailableException)
        {
            await InitializeCaches(
                blockIndex.Value,
                participantRepo,
                seasonRepo,
                seasonCacheRepo,
                rankingSnapshotRepo,
                rankingRepo,
                clanRankingRepo,
                groupRankingRepo
            );
        }

        await seasonCacheRepo.SetBlockIndexAsync(blockIndex.Value);

        _logger.LogInformation($"Block index: {blockIndex}");

        await HandleSeason(
            blockIndex.Value,
            seasonRepo,
            userRepo,
            participantRepo,
            rankingSnapshotRepo,
            rankingRepo,
            clanRankingRepo,
            groupRankingRepo,
            seasonCacheRepo
        );
        await HandleRound(
            blockIndex.Value,
            seasonRepo,
            rankingSnapshotRepo,
            rankingRepo,
            clanRankingRepo,
            groupRankingRepo,
            seasonCacheRepo
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

    private async Task HandleSeason(
        long blockIndex,
        ISeasonRepository seasonRepo,
        IUserRepository userRepo,
        IParticipantRepository participantRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        var cachedSeason = await seasonCacheRepo.GetSeasonAsync();

        if (blockIndex >= cachedSeason.EndBlock - 15)
        {
            // 시즌이 끝나기 15블록 전부터 다음 시즌에 대한 정보를 미리 주입해둡니다.
            // 이미 이니셜라이징을 했다면 진행하지 않습니다.
            var nextSeasonInfo = await GetCurrentSeasonInfo(blockIndex + 16, seasonRepo);
            if (
                !prepareSeasonInProgress
                && await rankingSnapshotRepo.GetRankingSnapshotsCount(
                    nextSeasonInfo.Season.Id,
                    nextSeasonInfo.Round.Id
                ) <= 0
            )
            {
                await PrepareNextSeason(
                    nextSeasonInfo,
                    userRepo,
                    participantRepo,
                    rankingSnapshotRepo,
                    rankingRepo,
                    clanRankingRepo,
                    groupRankingRepo
                );
            }
            else
            {
                _logger.LogInformation(
                    $"Season {nextSeasonInfo.Season.Id}: Already prepared season."
                );
            }
        }

        // 시즌 변경 탐지
        if (blockIndex < cachedSeason.StartBlock || blockIndex > cachedSeason.EndBlock)
        {
            // 다음 시즌에 대한 정보를 받아와서
            var seasonInfo = await GetCurrentSeasonInfo(blockIndex, seasonRepo);

            // 새 시즌 정보를 캐싱합니다
            await seasonCacheRepo.SetSeasonAsync(
                seasonInfo.Season.Id,
                seasonInfo.Season.StartBlock,
                seasonInfo.Season.EndBlock
            );
            return;
        }
    }

    private async Task HandleRound(
        long blockIndex,
        ISeasonRepository seasonRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo,
        ISeasonCacheRepository seasonCacheRepo
    )
    {
        var cachedRound = await seasonCacheRepo.GetRoundAsync();

        if (blockIndex >= cachedRound.EndBlock - 2)
        {
            // 라운드가 끝나기 2 블록 전에 다음 라운드 랭킹을 준비합니다.
            var nextRoundInfo = await GetCurrentSeasonInfo(blockIndex + 3, seasonRepo);
            if (
                !prepareRoundInProgress
                && !prepareSeasonInProgress
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
                    groupRankingRepo
                );
            }
            else
            {
                _logger.LogInformation($"Round {nextRoundInfo.Round.Id}: Already prepared round.");
            }
        }

        // 라운드 변경 탐지
        if (blockIndex < cachedRound.StartBlock || blockIndex > cachedRound.EndBlock)
        {
            // 다음 시즌에 대한 정보를 받아와서
            var seasonInfo = await GetCurrentSeasonInfo(blockIndex, seasonRepo);

            await seasonCacheRepo.SetRoundAsync(
                seasonInfo.Round.Id,
                seasonInfo.Round.StartBlock,
                seasonInfo.Round.EndBlock
            );
            return;
        }
    }

    private async Task PrepareNextSeason(
        (Season Season, Round Round) nextSeasonInfo,
        IUserRepository userRepo,
        IParticipantRepository participantRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo
    )
    {
        prepareSeasonInProgress = true;
        _logger.LogInformation($"Start PrepareNextSeason {nextSeasonInfo.Season.Id}");

        // 모든 유저 중 메달을 충족하는 유저를 가져옵니다.
        var users = await userRepo.GetAllUserAsync();
        // 새 시즌에 참가시킵니다.
        await participantRepo.AddParticipantsAsync(users, nextSeasonInfo.Season.Id);

        // 초기 랭킹 데이터를 만듭니다.
        var rankingData = users.Select(u => (u.AvatarAddress, 1000)).ToList();
        var clanRankingData = new Dictionary<int, int>();
        foreach (var user in users)
        {
            if (user.ClanId is not null)
            {
                try
                {
                    clanRankingData[user.ClanId.Value] += 1000;
                }
                catch (KeyNotFoundException)
                {
                    clanRankingData[user.ClanId.Value] = 1000;
                }
            }
        }

        // snapshot을 찍고 랭킹을 주입해줍니다. 최초 라운드와 +1한 라운드
        await rankingSnapshotRepo.AddRankingsSnapshot(
            rankingData,
            nextSeasonInfo.Season.Id,
            nextSeasonInfo.Round.Id
        );
        await rankingSnapshotRepo.AddClanRankingsSnapshot(
            clanRankingData.Select(x => (x.Key, x.Value)).ToList(),
            nextSeasonInfo.Season.Id,
            nextSeasonInfo.Round.Id
        );
        await InitializeNewSeasonRankings(
            rankingData,
            clanRankingData.Select(x => (x.Key, x.Value)).ToList(),
            nextSeasonInfo.Season,
            nextSeasonInfo.Round,
            rankingRepo,
            clanRankingRepo,
            groupRankingRepo
        );
        prepareSeasonInProgress = false;
        _logger.LogInformation($"PrepareNextSeason {nextSeasonInfo.Season.Id} Done");
    }

    private async Task PrepareNextRound(
        (Season Season, Round Round) nextRoundInfo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo
    )
    {
        prepareRoundInProgress = true;
        _logger.LogInformation($"Start PrepareNextRound {nextRoundInfo.Round.Id}");

        // 다음 라운드의 +1 한 라운드를 준비합니다.
        await rankingRepo.CopyRoundDataAsync(
            nextRoundInfo.Season.Id,
            nextRoundInfo.Round.Id,
            nextRoundInfo.Round.Id + 1,
            nextRoundInfo.Season.RoundInterval
        );
        await groupRankingRepo.CopyRoundDataAsync(
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

        prepareRoundInProgress = false;
        _logger.LogInformation($"PrepareNextRound {nextRoundInfo.Round.Id} Done");
    }

    private async Task InitializeNewSeasonRankings(
        List<(Address, int)> rankingData,
        List<(int, int)> clanRankingData,
        Season season,
        Round round,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo
    )
    {
        await rankingRepo.InitRankingAsync(rankingData, season.Id, round.Id, season.RoundInterval);
        await clanRankingRepo.InitRankingAsync(
            clanRankingData,
            season.Id,
            round.Id,
            season.RoundInterval
        );
        await groupRankingRepo.InitRankingAsync(
            rankingData,
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
        await groupRankingRepo.InitRankingAsync(
            rankingData,
            season.Id,
            round.Id + 1,
            season.RoundInterval
        );
    }

    private async Task<(Season Season, Round Round)> GetCurrentSeasonInfo(
        long blockIndex,
        ISeasonRepository seasonRepo
    )
    {
        var season = await seasonRepo.GetSeasonByBlockIndexAsync(
            blockIndex,
            q => q.Include(s => s.Rounds)
        );

        if (season == null)
        {
            _logger.LogError("No matching season found for the current block index.");
            throw new CacheUnavailableException(
                $"No matching season found for the current block index ({blockIndex})."
            );
        }

        var round = season.Rounds.FirstOrDefault(ai =>
            ai.StartBlock <= blockIndex && ai.EndBlock >= blockIndex
        );

        if (round == null)
        {
            throw new CacheUnavailableException(
                $"No matching round found for the current block index ({blockIndex})."
            );
        }

        return (season, round);
    }

    private async Task InitializeCaches(
        long blockIndex,
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IRankingSnapshotRepository rankingSnapshotRepo,
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo
    )
    {
        //  지금 진행되고 있는 시즌 정보를 받아옵니다.
        var seasonInfo = await GetCurrentSeasonInfo(blockIndex, seasonRepo);
        _logger.LogInformation($"InitializeCaches {seasonInfo.Season.Id} {seasonInfo.Round.Id}");

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
        await groupRankingRepo.InitRankingAsync(
            rankingData,
            seasonInfo.Season.Id,
            seasonInfo.Round.Id,
            seasonInfo.Season.RoundInterval
        );

        // 다음 라운드의 대한 정보는 지금 진행중인 score로 초기화합니다.
        var participants = await participantRepo.GetParticipantsAsync(
            seasonInfo.Season.Id,
            q => q.Include(p => p.User)
        );

        var nextRoundRankingData = participants.Select(r => (r.AvatarAddress, r.Score)).ToList();
        var nextRoundClanRankingData = new Dictionary<int, int>();
        foreach (var participant in participants)
        {
            if (participant.User.ClanId is not null)
            {
                try
                {
                    nextRoundClanRankingData[participant.User.ClanId.Value] += participant.Score;
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
        await groupRankingRepo.InitRankingAsync(
            nextRoundRankingData,
            seasonInfo.Season.Id,
            seasonInfo.Round.Id + 1,
            seasonInfo.Season.RoundInterval
        );

        // 시즌과 라운드를 캐싱합니다.
        await seasonCacheRepo.SetSeasonAsync(
            seasonInfo.Season.Id,
            seasonInfo.Season.StartBlock,
            seasonInfo.Season.EndBlock
        );
        await seasonCacheRepo.SetRoundAsync(
            seasonInfo.Round.Id,
            seasonInfo.Round.StartBlock,
            seasonInfo.Round.EndBlock
        );

        _logger.LogInformation(
            $"InitializeCaches {seasonInfo.Season.Id} {seasonInfo.Round.Id} Done"
        );
    }
}

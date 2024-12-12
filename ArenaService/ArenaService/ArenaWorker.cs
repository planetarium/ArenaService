using System.Diagnostics;
using ArenaService.Models;

namespace ArenaService;

public class ArenaParticipantsWorker : BackgroundService
{
    private RpcClient _rpcClient;
    private int _interval;
    private IRedisArenaParticipantsService _service;
    private ILogger<ArenaParticipantsWorker> _logger;
    private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public ArenaParticipantsWorker(RpcClient rpcClient, IRedisArenaParticipantsService service, ILogger<ArenaParticipantsWorker> logger)
    {
        _rpcClient = rpcClient;
        _service = service;
        _interval = 8;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_interval * 1000, stoppingToken);
                await PrepareArenaParticipants();
            }
        }
        finally
        {
            _cts.Dispose();
        }
    }

    /// <summary>
    /// Prepares the arena participants by syncing the arena cache.
    /// </summary>
    /// <returns>A <see cref="Task"/> completed when set arena cache.</returns>
    public async Task PrepareArenaParticipants()
    {
        _logger.LogInformation("[ArenaParticipantsWorker]Start Sync Arena Cache");
        var sw = new Stopwatch();
        sw.Start();
        // Copy from NineChronicles RxProps.Arena
        // https://github.com/planetarium/NineChronicles/blob/80.0.1/nekoyume/Assets/_Scripts/State/RxProps.Arena.cs#L279
        var retry = 0;
        _cts.CancelAfter(TimeSpan.FromMinutes(5));
        var cancellationToken = _cts.Token;
        while (_rpcClient.Tip?.Index == _rpcClient.PreviousTip?.Index)
        {
            await Task.Delay((5 - retry) * 1000, cancellationToken);
            retry++;
            if (retry >= 3)
            {
                throw new InvalidOperationException();
            }
        }

        var tip = _rpcClient.Tip!;
        var currentRoundData = await _rpcClient.GetRoundData(tip, cancellationToken);
        var participants = await _rpcClient.GetArenaParticipantsState(tip, currentRoundData, cancellationToken);
        var cacheKey = $"{currentRoundData.ChampionshipId}_{currentRoundData.Round}";
        var scoreCacheKey = $"{cacheKey}_scores";
        var prevAddrAndScores = await _service.GetAvatarAddrAndScores(scoreCacheKey);
        var prevArenaParticipants = await _service.GetArenaParticipantsAsync(cacheKey);
        var expiry = TimeSpan.FromMinutes(5);
        if (participants is null)
        {
            await _service.SetArenaParticipantsAsync(cacheKey, new List<ArenaParticipant>(), expiry);
            _logger.LogInformation("[ArenaParticipantsWorker] participants({CacheKey}) is null. set empty list", cacheKey);
            return;
        }

        var avatarAddrList = participants.AvatarAddresses;
        // 최신상태의 아바타 주소, 점수를 조회
        var avatarAddrAndScores = await _rpcClient.GetAvatarAddrAndScores(tip, avatarAddrList, currentRoundData, cancellationToken);
        // 이전상태의 아바타 주소, 점수를 비교해서 추가되거나 점수가 변경된 대상만 찾음
        var updatedAddressAndScores = avatarAddrAndScores.Except(prevAddrAndScores).ToList();
        // 전체목록의 랭킹 순서 처리
        var avatarAddrAndScoresWithRank = _rpcClient.AvatarAddrAndScoresWithRank(avatarAddrAndScores);
        // 전체목록의 ArenaParticipant 업데이트
        var result = await _rpcClient.GetArenaParticipants(tip, updatedAddressAndScores.Select(i => i.AvatarAddr).ToList(), avatarAddrAndScoresWithRank, prevArenaParticipants, cancellationToken);
        // 캐시 업데이트
        await _service.SetArenaParticipantsAsync(cacheKey, result, expiry);
        await _service.SetSeasonAsync(cacheKey, expiry);
        await _service.SetAvatarAddrAndScores(scoreCacheKey, avatarAddrAndScores, expiry);
        sw.Stop();
        _logger.LogInformation("[ArenaParticipantsWorker]Set Arena Cache[{CacheKey}]: {Elapsed}", cacheKey, sw.Elapsed);
    }
}

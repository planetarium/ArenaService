using System.Diagnostics;

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
        var blockIndex = tip.Index;
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
            _logger.LogInformation("[ArenaParticipantsWorker] participants({CacheKey}) is null. set empty list on {BlockIndex}", cacheKey, blockIndex);
            return;
        }

        var avatarAddrList = participants.AvatarAddresses;
        // 최신상태의 아바타 주소, 점수를 조회
        var avatarAddrAndScores = await _rpcClient.GetAvatarAddrAndScores(tip, avatarAddrList, currentRoundData, cancellationToken);
        // 이전상태의 아바타 주소, 점수를 비교해서 추가되거나 점수가 변경된 대상만 찾음
        var updatedAddressAndScores = avatarAddrAndScores.Except(prevAddrAndScores).ToList();
        // 전체목록의 랭킹 순서 처리
        var avatarAddrAndScoresWithRank = AvatarAddrAndScoresWithRank(avatarAddrAndScores);
        // 전체목록의 ArenaParticipant 업데이트
        var result = await _rpcClient.GetArenaParticipants(tip, updatedAddressAndScores.Select(i => i.AvatarAddr).ToList(), avatarAddrAndScoresWithRank, prevArenaParticipants, cancellationToken);
        // 캐시 업데이트
        await _service.SetArenaParticipantsAsync(cacheKey, result, expiry);
        await _service.SetSeasonAsync(cacheKey, expiry);
        await _service.SetAvatarAddrAndScores(scoreCacheKey, avatarAddrAndScores, expiry);
        sw.Stop();
        _logger.LogInformation("[ArenaParticipantsWorker]Set Arena Cache[{CacheKey}] on {BlockIndex}: {Elapsed}", cacheKey, blockIndex, sw.Elapsed);
    }


    /// <summary>
    /// Retrieves the avatar addresses and scores with ranks for a given list of avatar addresses, current round data, and world state.
    /// </summary>
    /// <param name="avatarAddrAndScores">Ths list of avatar address and score tuples.</param>
    /// <returns>The list of avatar addresses, scores, and ranks.</returns>
    public static List<ArenaScoreAndRank> AvatarAddrAndScoresWithRank(List<AvatarAddressAndScore> avatarAddrAndScores)
    {
        if (avatarAddrAndScores.Count == 0)
        {
            return new List<ArenaScoreAndRank>();
        }

        if (avatarAddrAndScores.Count == 1)
        {
            var score = avatarAddrAndScores.Single();
            return [new ArenaScoreAndRank(score.AvatarAddr, score.Score, 1)];
        }

        var orderedTuples = avatarAddrAndScores
            .OrderByDescending(tuple => tuple.Score)
            .ThenBy(tuple => tuple.AvatarAddr)
            .ToList();

        var avatarAddrAndScoresWithRank = new List<ArenaScoreAndRank>();
        while (orderedTuples.Count > 0)
        {
            // 동점자를 찾기위해 기준 점수 설정
            var currentScore = orderedTuples.First().Score;
            var groupSize = 0;
            var targets = new List<AvatarAddressAndScore>();
            foreach (var tuple in orderedTuples)
            {
                if (currentScore == tuple.Score)
                {
                    groupSize++;
                    targets.Add(tuple);
                }
                else
                {
                    break;
                }
            }

            // 순위는 기존 상위권 순위 + 동점자의 숫자
            var rank = avatarAddrAndScoresWithRank.Count + groupSize;
            avatarAddrAndScoresWithRank.AddRange(targets.Select(tuple => new ArenaScoreAndRank(tuple.AvatarAddr, tuple.Score, rank)));
            // 다음 순위 설정을 위해 이번 그룹 숫자만큼 삭제
            orderedTuples.RemoveRange(0, groupSize);
        }

        return avatarAddrAndScoresWithRank;
    }
}

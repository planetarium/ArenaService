using System.Diagnostics;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Model.Arena;
using Nekoyume.Module;
using Nekoyume.TableData;

namespace ArenaService;

public class ArenaParticipantsWorker : BackgroundService
{
    private RpcClient _rpcClient;
    private int _interval;
    private IRedisArenaParticipantsService _service;
    private ILogger<ArenaParticipantsWorker> _logger;

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
                await Task.Delay(_interval, stoppingToken);
                await PrepareArenaParticipants();
            }
        }
        catch (OperationCanceledException)
        {
            //pass
            _logger.LogInformation("[ArenaParticipantsWorker]Cancel ArenaParticipantsWorker");
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
        while (_rpcClient.Tip?.Index == _rpcClient.PreviousTip?.Index)
        {
            await Task.Delay((5 - retry) * 1000);
            retry++;
            if (retry >= 3)
            {
                throw new InvalidOperationException();
            }
        }

        var tip = _rpcClient.Tip;
        var currentRoundData = await _rpcClient.GetRoundData(tip);
        var participants = await _rpcClient.GetArenaParticipantsState(tip, currentRoundData);
        var cacheKey = $"{currentRoundData.ChampionshipId}_{currentRoundData.Round}";
        var scoreCacheKey = $"{cacheKey}_score";
        if (participants is null)
        {
            await _service.SetArenaParticipantsAsync(cacheKey, new List<ArenaParticipant>());
            _logger.LogInformation("[ArenaParticipantsWorker] participants({CacheKey}) is null. set empty list", cacheKey);
            return;
        }

        var avatarAddrList = participants.AvatarAddresses;
        var avatarAddrAndScoresWithRank = await _rpcClient.AvatarAddrAndScoresWithRank(tip, avatarAddrList, currentRoundData);
        var result = await _rpcClient.GetArenaParticipants(tip, avatarAddrList, avatarAddrAndScoresWithRank);
        await _service.SetArenaParticipantsAsync(cacheKey, result, TimeSpan.FromHours(1));
        await _service.SetAvatarAddrAndScoresWithRank(scoreCacheKey, avatarAddrAndScoresWithRank,
            TimeSpan.FromHours(1));
        await _service.SetSeasonAsync(cacheKey, TimeSpan.FromHours(1));
        sw.Stop();
        _logger.LogInformation("[ArenaParticipantsWorker]Set Arena Cache[{CacheKey}]: {Elapsed}", cacheKey, sw.Elapsed);
    }
}

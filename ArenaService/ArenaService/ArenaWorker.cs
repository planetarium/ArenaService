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
            _ = _rpcClient.InitializeAsync(stoppingToken);
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
        catch (Exception e)
        {
            _logger.LogError(e, "[ArenaParticipantsWorker]Stopping ArenaParticipantsWorker");
        }
    }

    /// <summary>
    /// Retrieves the state of arena participants from the given world state and current round data.
    /// </summary>
    /// <param name="worldState">The world state.</param>
    /// <param name="currentRoundData">The current round data.</param>
    /// <returns>The arena participants state, or null if not found.</returns>
    public static ArenaParticipants? GetArenaParticipantsState(IWorldState worldState, ArenaSheet.RoundData currentRoundData)
    {
        var participantsAddr = ArenaParticipants.DeriveAddress(
            currentRoundData.ChampionshipId,
            currentRoundData.Round);
        var participants = worldState.GetLegacyState(participantsAddr) is List participantsList
            ? new ArenaParticipants(participantsList)
            : null;
        return participants;
    }

    /// <summary>
    /// Gets the round data from the specified world state and block index.
    /// </summary>
    /// <param name="worldState">The world state containing the arena sheet.</param>
    /// <param name="blockIndex">The block index for which to retrieve the round data.</param>
    /// <returns>The round data for the specified block index.</returns>
    public static ArenaSheet.RoundData GetRoundData(IWorldState worldState, long blockIndex)
    {
        return worldState.GetSheet<ArenaSheet>().GetRoundByBlockIndex(blockIndex);
    }

    /// <summary>
    /// Retrieves the avatar addresses and scores with ranks for a given list of avatar addresses, current round data, and world state.
    /// </summary>
    /// <param name="avatarAddrList">The list of avatar addresses.</param>
    /// <param name="currentRoundData">The current round data.</param>
    /// <param name="worldState">The world state.</param>
    /// <returns>The list of avatar addresses, scores, and ranks.</returns>
    public static List<(Address avatarAddr, int score, int rank)> AvatarAddrAndScoresWithRank(List<Address> avatarAddrList, ArenaSheet.RoundData currentRoundData, IWorldState worldState)
    {
        var avatarAndScoreAddrList = avatarAddrList
            .Select(avatarAddr => (
                avatarAddr,
                ArenaScore.DeriveAddress(
                    avatarAddr,
                    currentRoundData.ChampionshipId,
                    currentRoundData.Round)))
            .ToArray();
        // NOTE: If addresses is too large, and split and get separately.
        var scores = worldState.GetLegacyStates(
            avatarAndScoreAddrList.Select(tuple => tuple.Item2).ToList());
        var avatarAddrAndScores = new List<(Address avatarAddr, int score)>();
        for (int i = 0; i < avatarAddrList.Count; i++)
        {
            var tuple = avatarAndScoreAddrList[i];
            var score = scores[i] is List scoreList ? (int)(Integer)scoreList[1] : ArenaScore.ArenaScoreDefault;
            avatarAddrAndScores.Add((tuple.avatarAddr, score));
        }

        List<(Address avatarAddr, int score, int rank)> orderedTuples = avatarAddrAndScores
            .OrderByDescending(tuple => tuple.score)
            .ThenBy(tuple => tuple.avatarAddr)
            .Select(tuple => (tuple.avatarAddr, tuple.score, 0))
            .ToList();
        int? currentScore = null;
        var currentRank = 1;
        var avatarAddrAndScoresWithRank = new List<(Address avatarAddr, int score, int rank)>();
        var trunk = new List<(Address avatarAddr, int score, int rank)>();
        for (var i = 0; i < orderedTuples.Count; i++)
        {
            var tuple = orderedTuples[i];
            if (!currentScore.HasValue)
            {
                currentScore = tuple.score;
                trunk.Add(tuple);
                continue;
            }

            if (currentScore.Value == tuple.score)
            {
                trunk.Add(tuple);
                currentRank++;
                if (i < orderedTuples.Count - 1)
                {
                    continue;
                }

                foreach (var tupleInTrunk in trunk)
                {
                    avatarAddrAndScoresWithRank.Add((
                        tupleInTrunk.avatarAddr,
                        tupleInTrunk.score,
                        currentRank));
                }

                trunk.Clear();

                continue;
            }

            foreach (var tupleInTrunk in trunk)
            {
                avatarAddrAndScoresWithRank.Add((
                    tupleInTrunk.avatarAddr,
                    tupleInTrunk.score,
                    currentRank));
            }

            trunk.Clear();
            if (i < orderedTuples.Count - 1)
            {
                trunk.Add(tuple);
                currentScore = tuple.score;
                currentRank++;
                continue;
            }

            avatarAddrAndScoresWithRank.Add((
                tuple.avatarAddr,
                tuple.score,
                currentRank + 1));
        }

        return avatarAddrAndScoresWithRank;
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
        while (_rpcClient.Tip is null)
        {
            await Task.Delay(1000);
        }

        var tip = _rpcClient.Tip;
        var currentRoundData = await _rpcClient.GetRoundData(tip);
        var participants = await _rpcClient.GetArenaParticipantsState(tip, currentRoundData);
        var cacheKey = $"{currentRoundData.ChampionshipId}_{currentRoundData.Round}";
        if (participants is null)
        {
            await _service.SetValueAsync(cacheKey, new List<ArenaParticipant>());
            _logger.LogInformation("[ArenaParticipantsWorker] participants({CacheKey}) is null. set empty list", cacheKey);
            return;
        }

        var avatarAddrList = participants.AvatarAddresses;
        var avatarAddrAndScoresWithRank = await _rpcClient.AvatarAddrAndScoresWithRank(tip, avatarAddrList, currentRoundData);
        var result = await _rpcClient.GetArenaParticipants(tip, avatarAddrList, avatarAddrAndScoresWithRank);
        await _service.SetValueAsync(cacheKey, result, TimeSpan.FromHours(1));
        sw.Stop();
        _logger.LogInformation("[ArenaParticipantsWorker]Set Arena Cache[{CacheKey}]: {Elapsed}", cacheKey, sw.Elapsed);
    }
}

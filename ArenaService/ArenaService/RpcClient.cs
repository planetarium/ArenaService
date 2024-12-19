using System.Collections.Concurrent;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using MagicOnion.Client;
using Nekoyume;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;

namespace ArenaService;

public class RpcClient: IDisposable, IActionEvaluationHubReceiver
{
    public Address Address => _privateKey.Address;
    public Block PreviousTip;
    public Block Tip;
    public bool Ready;
    private readonly PrivateKey _privateKey;
    private IBlockChainService _service;
    private IActionEvaluationHub _hub;
    private readonly Codec _codec;
    private string _rpcHost;
    private bool _selfDisconnect;
    private IReadOnlyList<string> _rpcHosts;
    private ICollection<string> _failedRpcHosts = new List<string>();

    public RpcClient(PrivateKey privateKey, IConfiguration configuration)
    {
        _privateKey = privateKey;
        _codec = new Codec();
        _rpcHosts = configuration.GetSection("Rpc:Host").Get<List<string>>()!;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _selfDisconnect = true;
                cancellationToken.ThrowIfCancellationRequested();
            }

            try
            {
                await Join(cancellationToken);
            }
            catch (Exception)
            {
                Ready = false;
            }

            if (_selfDisconnect)
            {
                break;
            }
        }

        await Task.CompletedTask;
    }

    private async Task Join(CancellationToken cancellationToken)
    {
        _rpcHost =  _rpcHosts.First(r => !_failedRpcHosts.Contains(r));
        var channel = GrpcChannel.ForAddress(_rpcHost,
            new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = null,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                }
            }
        );

        try
        {
            _hub = await StreamingHubClient.ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                channel,
                this,
                cancellationToken: cancellationToken);
        }
        catch (RpcException)
        {
            _failedRpcHosts.Add(_rpcHost);
            await Join(cancellationToken);
        }
        _service = MagicOnionClient.Create<IBlockChainService>(channel)
            .WithCancellationToken(cancellationToken);

        await _hub.JoinAsync(Address.ToHex());
        await _service.AddClient(Address.ToByteArray());
        Ready = true;

        await _hub.WaitForDisconnect();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _hub.LeaveAsync();
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void OnRender(byte[] evaluation)
    {
        //pass
    }

    public void OnUnrender(byte[] evaluation)
    {
        //pass
    }

    public void OnRenderBlock(byte[] oldTip, byte[] newTip)
    {
        var dict = (Dictionary)_codec.Decode(newTip);
        var newTipBlock = BlockMarshaler.UnmarshalBlock(dict);
        PreviousTip = Tip;
        Tip = newTipBlock;
    }

    public void OnReorged(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
        //pass
    }

    public void OnReorgEnd(byte[] oldTip, byte[] newTip, byte[] branchpoint)
    {
        //pass
    }

    public void OnException(int code, string message)
    {
        //pass
    }

    public void OnPreloadStart()
    {
        //pass
    }

    public void OnPreloadEnd()
    {
        //pass
    }


    /// <summary>
    /// Get <see cref="ISheet"/> by block index.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="ISheet"/></returns>
    public async Task<T> GetSheet<T>(Block block, CancellationToken cancellationToken) where T : ISheet, new()
    {
        var address = Addresses.GetSheetAddress<T>();
        var result = await _service
            .WithCancellationToken(cancellationToken)
            .GetStateByStateRootHash(
                block.StateRootHash.ToByteArray(),
                ReservedAddresses.LegacyAccount.ToByteArray(),
                address.ToByteArray()
            );
        if (_codec.Decode(result) is Text t)
        {
            var sheet = new T();
            sheet.Set(t);
            return sheet;
        }

        throw new Exception();
    }

    /// <summary>
    /// Get <see cref="IValue"/> from LegacyAccount by block index.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="address"><see cref="Address"/>state address</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="IValue"/> or null if not found.</returns>
    public async Task<IValue?> GetLegacyState(Block block, Address address, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        return await GetState(
            block,
            ReservedAddresses.LegacyAccount,
            address,
            cancellationToken);
    }

    /// <summary>
    /// Gets the round data from specified block.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="ArenaSheet.RoundData"/>round data</returns>
    public async Task<ArenaSheet.RoundData> GetRoundData(Block block, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var arenaSheet = await GetSheet<ArenaSheet>(block, cancellationToken);
        return arenaSheet.GetRoundByBlockIndex(block.Index);
    }

    /// <summary>
    /// Retrieves the state of arena participants from the given world state and current round data.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="currentRoundData"><see cref="ArenaSheet.RoundData"/>The current round data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The arena participants state, or null if not found.</returns>
    public async Task<ArenaParticipants?> GetArenaParticipantsState(Block block, ArenaSheet.RoundData currentRoundData,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var participantsAddr = ArenaParticipants.DeriveAddress(
            currentRoundData.ChampionshipId,
            currentRoundData.Round);
        var participants = await GetLegacyState(block, participantsAddr, cancellationToken) is List participantsList
            ? new ArenaParticipants(participantsList)
            : null;
        return participants;
    }

    public async Task<List<AvatarAddressAndScore>> GetAvatarAddrAndScores(Block block, List<Address> avatarAddrList,
        ArenaSheet.RoundData currentRoundData, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var avatarAndScoreAddrList = avatarAddrList
            .Select(avatarAddr => (
                avatarAddr,
                ArenaScore.DeriveAddress(
                    avatarAddr,
                    currentRoundData.ChampionshipId,
                    currentRoundData.Round)))
            .ToArray();
        var scores = await GetStates(
            block,
            ReservedAddresses.LegacyAccount,
            avatarAndScoreAddrList.Select(tuple => tuple.Item2).ToList(),
            cancellationToken);
        var avatarAddrAndScores = new List<AvatarAddressAndScore>();
        foreach (var tuple in avatarAndScoreAddrList)
        {
            var scoreAddress = tuple.Item2;
            var score = scores[scoreAddress] is List scoreList ? (int)(Integer)scoreList[1] : ArenaScore.ArenaScoreDefault;
            avatarAddrAndScores.Add(new AvatarAddressAndScore(tuple.avatarAddr, score));
        }

        return avatarAddrAndScores;
    }

    /// <summary>
    /// Retrieve a list of arena participants based on the provided world state, avatar address list, and avatar addresses with scores and ranks.
    /// </summary>
    /// <param name="block"><see cref="Block"/> from which to retrieve the arena participants.</param>
    /// <param name="championshipId">target arena season id</param>
    /// <param name="round">target arena season round</param>
    /// <param name="avatarAddrList">The list of avatar addresses to filter the matching participants.</param>
    /// <param name="avatarAddrAndScoresWithRank">The list of avatar addresses with their scores and ranks.</param>
    /// <param name="prevArenaParticipants">The list of previous synced arena participants. if the score has not changed, <see cref="ArenaParticipantStruct"/> is reused.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>A list of arena participants.</returns>
    public async Task<List<ArenaParticipantStruct>> GetArenaParticipants(Block block, int championshipId, int round, List<Address> avatarAddrList,
        List<ArenaScoreAndRank> avatarAddrAndScoresWithRank, List<ArenaParticipantStruct> prevArenaParticipants,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        var arenaParticipantStates = await GetArenaParticipantStates(block, championshipId, round, avatarAddrList, cancellationToken);
        var tasks = avatarAddrAndScoresWithRank.Select(async tuple =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var avatarAddr = tuple.AvatarAddr;
            // 점수가 변경된 경우, BattleArena를 실행한 아바타기때문에 전체 정보를 업데이트한다.
            if (avatarAddrList.Contains(avatarAddr))
            {
                var arenaParticipantState = arenaParticipantStates[avatarAddr];
                return new ArenaParticipantStruct(
                    avatarAddr,
                    tuple.Score,
                    tuple.Rank,
                    $"{arenaParticipantState.Name} <size=80%><color=#A68F7E>#{avatarAddr.ToHex().Substring(0, 4)}</color></size>",
                    arenaParticipantState.Level,
                    arenaParticipantState.PortraitId,
                    0,
                    0,
                    arenaParticipantState.Cp
                );
            }

            // 점수가 그대로인 경우, 순위만 변경한다.
            var prev = prevArenaParticipants.First(r => r.AvatarAddr == avatarAddr);
            return new ArenaParticipantStruct(
                avatarAddr,
                tuple.Score,
                tuple.Rank,
                prev.NameWithHash,
                prev.Level,
                prev.PortraitId,
                0,
                0,
                prev.Cp
            );
        }).ToList();
        var result = await Task.WhenAll(tasks);
        return result.ToList();
    }

    /// <summary>
    /// Split and get <see cref="IValue"/> separately by given chunkSize.
    /// </summary>
    /// <param name="block"><see cref="Block"/></param>
    /// <param name="accountAddress">target account address.</param>
    /// <param name="addresses">list of target state address.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="chunkSize">chunking size. default value is 500</param>
    /// <returns>A dictionary of Address and IValue pairs for the given addresses.</returns>
    public async Task<Dictionary<Address, IValue>> GetStates(Block block, Address accountAddress,
        IReadOnlyList<Address> addresses, CancellationToken cancellationToken, int chunkSize = 500)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var result = new ConcurrentDictionary<Address, IValue>();
        var chunks = addresses
            .Select((x, i) => new {Index = i, Value = x})
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value.ToByteArray()).ToList())
            .ToList();
        foreach (var chunk in chunks)
        {
            var queryResult = await _service.WithCancellationToken(cancellationToken).GetBulkStateByStateRootHash(block.StateRootHash.ToByteArray(), accountAddress.ToByteArray(), chunk);
            foreach (var kv in queryResult) result[new Address(kv.Key)] = _codec.Decode(kv.Value);
        }
        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Get <see cref="IValue"/> from account by block index.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="accountAddress"><see cref="Address"/>target account address</param>
    /// <param name="address"><see cref="Address"/>state address</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="IValue"/> or null if not found.</returns>
    public async Task<IValue?> GetState(Block block, Address accountAddress, Address address,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        byte[] result = [];
        var retry = 0;
        while (retry < 3)
        {
            try
            {
                result = await _service
                    .WithCancellationToken(cancellationToken)
                    .GetStateByStateRootHash(
                        block.StateRootHash.ToByteArray(),
                        accountAddress.ToByteArray(),
                        address.ToByteArray()
                    );
                break;
            }
            catch (RpcException)
            {
                await Task.Delay((3 - retry) * 1000, cancellationToken);
                retry++;
            }
        }
        if (_codec.Decode(result) is { } decode and not Null)
        {
            return decode;
        }

        return null;
    }

    /// <summary>
    /// Get <see cref="ArenaParticipant"/> from given arena season.
    /// </summary>
    /// <param name="block"><see cref="Block"/> from which to retrieve the arena participants.</param>
    /// <param name="championshipId">target arena season id</param>
    /// <param name="round">target arena season round</param>
    /// <param name="avatarAddresses">The list of avatar addresses to filter the matching participants.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of Address and <see cref="ArenaParticipant"/> pairs for the given addresses.</returns>
    public async Task<Dictionary<Address, ArenaParticipant>> GetArenaParticipantStates(Block block, int championshipId, int round, IReadOnlyList<Address> avatarAddresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var accountAddress = Addresses.GetArenaParticipantAccountAddress(championshipId, round);
        var serializedResults = await GetStates(block, accountAddress, avatarAddresses, cancellationToken);
        var result = new Dictionary<Address, ArenaParticipant>();
        foreach (var address in avatarAddresses)
        {
            var serialized = serializedResults[address];
            if (serialized is List l)
            {
                var arenaParticipant = new ArenaParticipant(l);
                result.TryAdd(address, arenaParticipant);
            }
        }
        return result;
    }

    public static List<RuneOptionSheet.Row.RuneOptionInfo> GetRuneOptions(
        List<RuneState> runeStates,
        RuneOptionSheet sheet)
    {
        var result = new List<RuneOptionSheet.Row.RuneOptionInfo>();
        foreach (var runeState in runeStates)
        {
            if (!sheet.TryGetValue(runeState.RuneId, out var row))
            {
                continue;
            }

            if (!row.LevelOptionMap.TryGetValue(runeState.Level, out var statInfo))
            {
                continue;
            }

            result.Add(statInfo);
        }

        return result;
    }

    public static int GetPortraitId(List<Equipment?> equipments, List<Costume?> costumes)
    {
        var fullCostume = costumes.FirstOrDefault(x => x?.ItemSubType == ItemSubType.FullCostume);
        if (fullCostume != null)
        {
            return fullCostume.Id;
        }

        var armor = equipments.FirstOrDefault(x => x?.ItemSubType == ItemSubType.Armor);
        return armor?.Id ?? GameConfig.DefaultAvatarArmorId;
    }
}

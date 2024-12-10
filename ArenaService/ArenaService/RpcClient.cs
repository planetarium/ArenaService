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
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;
using Nekoyume.TableData.Rune;

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

    /// <summary>
    /// Retrieves the avatar addresses and scores with ranks for a given list of avatar addresses, current round data, and world state.
    /// </summary>
    /// <param name="avatarAddrAndScores">Ths list of avatar address and score tuples.</param>
    /// <returns>The list of avatar addresses, scores, and ranks.</returns>
    public List<ArenaScoreAndRank> AvatarAddrAndScoresWithRank(List<AvatarAddressAndScore> avatarAddrAndScores)
    {
        List<ArenaScoreAndRank> orderedTuples = avatarAddrAndScores
            .OrderByDescending(tuple => tuple.Score)
            .ThenBy(tuple => tuple.AvatarAddr)
            .Select(tuple => new ArenaScoreAndRank(tuple.AvatarAddr, tuple.Score, 0))
            .ToList();
        int? currentScore = null;
        var currentRank = 1;
        var avatarAddrAndScoresWithRank = new List<ArenaScoreAndRank>();
        var trunk = new List<ArenaScoreAndRank>();
        for (var i = 0; i < orderedTuples.Count; i++)
        {
            var tuple = orderedTuples[i];
            if (!currentScore.HasValue)
            {
                currentScore = tuple.Score;
                trunk.Add(tuple);
                continue;
            }

            if (currentScore.Value == tuple.Score)
            {
                trunk.Add(tuple);
                currentRank++;
                if (i < orderedTuples.Count - 1)
                {
                    continue;
                }

                foreach (var tupleInTrunk in trunk)
                {
                    avatarAddrAndScoresWithRank.Add(new ArenaScoreAndRank(
                        tupleInTrunk.AvatarAddr,
                        tupleInTrunk.Score,
                        currentRank));
                }

                trunk.Clear();

                continue;
            }

            foreach (var tupleInTrunk in trunk)
            {
                avatarAddrAndScoresWithRank.Add(new ArenaScoreAndRank(
                    tupleInTrunk.AvatarAddr,
                    tupleInTrunk.Score,
                    currentRank));
            }

            trunk.Clear();
            if (i < orderedTuples.Count - 1)
            {
                trunk.Add(tuple);
                currentScore = tuple.Score;
                currentRank++;
                continue;
            }

            avatarAddrAndScoresWithRank.Add(new ArenaScoreAndRank(
                tuple.AvatarAddr,
                tuple.Score,
                currentRank + 1));
        }

        return avatarAddrAndScoresWithRank;
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
    /// <param name="avatarAddrList">The list of avatar addresses to filter the matching participants.</param>
    /// <param name="avatarAddrAndScoresWithRank">The list of avatar addresses with their scores and ranks.</param>
    /// <param name="prevArenaParticipants">The list of previous synced arena participants. if the score has not changed, <see cref="ArenaParticipant"/> is reused.</param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="Task"/>A list of arena participants.</returns>
    public async Task<List<ArenaService.Models.ArenaParticipant>> GetArenaParticipants(Block block, List<Address> avatarAddrList,
        List<ArenaScoreAndRank> avatarAddrAndScoresWithRank, List<ArenaService.Models.ArenaParticipant> prevArenaParticipants,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var runeListSheet = await GetSheet<RuneListSheet>(block, cancellationToken);
        var costumeSheet = await GetSheet<CostumeStatSheet>(block, cancellationToken);
        var characterSheet = await GetSheet<CharacterSheet>(block, cancellationToken);
        var runeOptionSheet = await GetSheet<RuneOptionSheet>(block, cancellationToken);
        var runeLevelBonusSheet = await GetSheet<RuneLevelBonusSheet>(block, cancellationToken);
        var row = characterSheet[GameConfig.DefaultAvatarCharacterId];
        CollectionSheet collectionSheet = new CollectionSheet();
        var collectionStates = await GetCollectionStates(block, avatarAddrList, cancellationToken);
        bool collectionSheetExist = true;
        try
        {
            collectionSheet = await GetSheet<CollectionSheet>(block, cancellationToken);
        }
        catch (Exception)
        {
            collectionSheetExist = false;
        }

        var itemSlotStates = await GetItemSlotStates(block, avatarAddrList, cancellationToken);
        var runeSlotStates = await GetRuneSlotStates(block, avatarAddrList, cancellationToken);
        var avatarStates = await GetAvatarStates(block, avatarAddrList, cancellationToken);
        var allRuneStates = await GetAllRuneStates(block, avatarAddrList, cancellationToken);
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
                if (!allRuneStates.TryGetValue(avatarAddr, out var runeStates))
                {
                    runeStates = await GetRuneState(block, avatarAddr, runeListSheet, cancellationToken);
                }
                var avatar = avatarStates[avatarAddr];
                var itemSlotState = itemSlotStates[avatarAddr];
                var runeSlotState = runeSlotStates[avatarAddr];

                var equippedRuneStates = new List<RuneState>();
                foreach (var runeId in runeSlotState.GetRuneSlot().Select(slot => slot.RuneId))
                {
                    if (!runeId.HasValue)
                    {
                        continue;
                    }

                    if (runeStates.TryGetRuneState(runeId.Value, out var runeState))
                    {
                        equippedRuneStates.Add(runeState);
                    }
                }

                var equipments = itemSlotState.Equipments
                    .Select(guid =>
                        avatar.inventory.Equipments.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                var costumes = itemSlotState.Costumes
                    .Select(guid =>
                        avatar.inventory.Costumes.FirstOrDefault(x => x.ItemId == guid))
                    .Where(item => item != null).ToList();
                var runeOptions = GetRuneOptions(equippedRuneStates, runeOptionSheet);
                var collectionExist = collectionStates.ContainsKey(avatarAddr);
                var collectionModifiers = new List<StatModifier>();
                if (collectionSheetExist && collectionExist)
                {
                    var collectionState = collectionStates[avatarAddr];
                    foreach (var collectionId in collectionState.Ids)
                    {
                        collectionModifiers.AddRange(collectionSheet[collectionId].StatModifiers);
                    }
                }

                var cp = CPHelper.TotalCP(equipments, costumes, runeOptions, avatar.level, row, costumeSheet, collectionModifiers,
                    RuneHelper.CalculateRuneLevelBonus(runeStates, runeListSheet, runeLevelBonusSheet)
                );
                var portraitId = GetPortraitId(equipments, costumes);
                return new ArenaService.Models.ArenaParticipant(
                    avatarAddr,
                    tuple.Score,
                    tuple.Rank,
                    avatar.NameWithHash,
                    avatar.level,
                    portraitId,
                    0,
                    0,
                    cp
                );
            }

            // 점수가 그대로인 경우, 순위만 변경한다.
            var prev = prevArenaParticipants.First(r => r.AvatarAddr == avatarAddr);
            return new ArenaService.Models.ArenaParticipant(
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
    /// Retrieves the collection states for the given addresses from the world state.
    /// </summary>
    /// <param name="block">The world state used to retrieve the collection states.</param>
    /// <param name="addresses">The list of addresses to retrieve the collection states for.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of Address and CollectionState pairs representing the collection states
    /// for the given addresses,
    /// or an empty dictionary for addresses that do not have a collection state.</returns>
    public async Task<Dictionary<Address, CollectionState>> GetCollectionStates(Block block, IReadOnlyList<Address> addresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var result = new Dictionary<Address, CollectionState>();
        var values = await GetStates(block, Addresses.Collection, addresses, cancellationToken);
        foreach (var address in addresses)
        {
            var serialized = values[address];
            if (serialized is List bencoded)
            {
                result.TryAdd(address, new CollectionState(bencoded));
            }
        }

        return result;
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

    public async Task<AllRuneState> GetRuneState(Block block, Address avatarAddress, RuneListSheet runeListSheet,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var serialized = await GetState(block, Addresses.RuneState, avatarAddress, cancellationToken);
        AllRuneState allRuneState;
        if (serialized is null)
        {
            // Get legacy rune states
            allRuneState = new AllRuneState();
            var runeAddresses = runeListSheet.Values.Select(r => RuneState.DeriveAddress(avatarAddress, r.Id)).ToList();
            var runeStates = await GetRuneStates(block, runeAddresses, cancellationToken);
            foreach (var runeState in runeStates)
            {
                allRuneState.AddRuneState(runeState);
            }
        }
        else
        {
            allRuneState = new AllRuneState((List)serialized);
        }

        return allRuneState;
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
    /// Retrieves the item slot states for the given avatar addresses from the world state.
    /// </summary>
    /// <param name="block">The world state used to retrieve the collection states.</param>
    /// <param name="avatarAddresses">The list of addresses to retrieve the item slot states for.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of Address and <see cref="ItemSlotState"/> pairs representing the item slot states
    /// for the given addresses.</returns>
    public async Task<IDictionary<Address, ItemSlotState>> GetItemSlotStates(Block block,
        IReadOnlyList<Address> avatarAddresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var result = new Dictionary<Address, ItemSlotState>();
        var slotAddresses = avatarAddresses.Select(a => ItemSlotState.DeriveAddress(a, BattleType.Arena)).ToList();
        var values = await GetStates(block, ReservedAddresses.LegacyAccount, slotAddresses, cancellationToken);
        foreach (var address in avatarAddresses)
        {
            var slotAddress = ItemSlotState.DeriveAddress(address, BattleType.Arena);
            var serialized = values[slotAddress];
            var itemSlotState = serialized is List bencoded
                ? new ItemSlotState(bencoded)
                : new ItemSlotState(BattleType.Arena);
            result.TryAdd(address, itemSlotState);
        }
        return result;
    }

    /// <summary>
    /// Retrieves the rune slot states for the given avatar addresses from the world state.
    /// </summary>
    /// <param name="block">The world state used to retrieve the collection states.</param>
    /// <param name="avatarAddresses">The list of avatar addresses to retrieve the rune slot states for.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A dictionary of Address and <see cref="RuneSlotState"/> pairs representing the rune slot states
    /// for the given addresses.</returns>
    public async Task<IDictionary<Address, RuneSlotState>> GetRuneSlotStates(Block block,
        IReadOnlyList<Address> avatarAddresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var result = new Dictionary<Address, RuneSlotState>();
        var slotAddresses = avatarAddresses.Select(a => RuneSlotState.DeriveAddress(a, BattleType.Arena)).ToList();
        var values = await GetStates(block, ReservedAddresses.LegacyAccount, slotAddresses, cancellationToken);
        foreach (var address in avatarAddresses)
        {
            var slotAddress = RuneSlotState.DeriveAddress(address, BattleType.Arena);
            var serialized = values[slotAddress];
            var runeSlotState = serialized is List bencoded
                ? new RuneSlotState(bencoded)
                : new RuneSlotState(BattleType.Arena);
            result.TryAdd(address, runeSlotState);
        }
        return result;
    }

    public async Task<IDictionary<Address, AvatarState>> GetAvatarStates(Block block,
        IReadOnlyList<Address> avatarAddresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var avatarResults = await GetStates(block, Addresses.Avatar, avatarAddresses, cancellationToken);
        var inventoryResults = await GetStates(block, Addresses.Inventory, avatarAddresses, cancellationToken);
        var result = new Dictionary<Address, AvatarState>();
        foreach (var kv in avatarResults)
        {
            var address = kv.Key;
            var avatarResult = kv.Value;
            if (avatarResult is List l)
            {
                var avatarState = new AvatarState(l);
                var inventory = inventoryResults[address] is List l2
                    ? new Inventory(l2)
                    : new Inventory();
                avatarState.inventory = inventory;
                result.TryAdd(address, avatarState);
            }
        }
        return result;
    }

    public async Task<List<RuneState>> GetRuneStates(Block block, IReadOnlyList<Address> runeAddresses,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var result = new List<RuneState>();
        var runeResults = await GetStates(block, ReservedAddresses.LegacyAccount, runeAddresses, cancellationToken);
        foreach (var pair in runeResults)
        {
            if (pair.Value is List rawState)
            {
                var runeState = new RuneState(rawState);
                result.Add(runeState);
            }
        }
        return result;
    }

    public async Task<IDictionary<Address, AllRuneState>> GetAllRuneStates(Block block,
        IReadOnlyList<Address> avatarAddresses, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
        var serializedResults = await GetStates(block, Addresses.RuneState, avatarAddresses, cancellationToken);
        var result = new Dictionary<Address, AllRuneState>();
        foreach (var address in avatarAddresses)
        {
            var serialized = serializedResults[address];
            if (serialized is List l)
            {
                var allRuneState = new AllRuneState(l);
                result.TryAdd(address, allRuneState);
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

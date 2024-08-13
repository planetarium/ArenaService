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
    private const int MaxDegreeOfParallelism = 8;

    public Address Address => _privateKey.Address;
    public Block Tip;
    private readonly PrivateKey _privateKey;
    private IBlockChainService _service;
    private IActionEvaluationHub _hub;
    private readonly Codec _codec;
    private readonly string _rpcHost;

    public RpcClient(PrivateKey privateKey, IConfiguration configuration)
    {
        _privateKey = privateKey;
        _codec = new Codec();
        _rpcHost = configuration["Rpc:Host"]!;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var channel = GrpcChannel.ForAddress(_rpcHost,
                    new GrpcChannelOptions
                    {
                        Credentials = ChannelCredentials.Insecure,
                        MaxReceiveMessageSize = null,
                        HttpHandler = new SocketsHttpHandler
                        {
                            EnableMultipleHttp2Connections = true,
                        }
                    }
                );

                _hub = await StreamingHubClient.ConnectAsync<IActionEvaluationHub, IActionEvaluationHubReceiver>(
                    channel,
                    this,
                    cancellationToken: cancellationToken);
                _service = MagicOnionClient.Create<IBlockChainService>(channel)
                    .WithCancellationToken(cancellationToken);

                await _hub.JoinAsync(Address.ToHex());
                await _service.AddClient(Address.ToByteArray());

                await _hub.WaitForDisconnect();
            }
        }, cancellationToken);

        await Task.CompletedTask;
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
    /// <returns><see cref="ISheet"/></returns>
    public async Task<T> GetSheet<T>(Block block) where T : ISheet, new()
    {
        var address = Addresses.GetSheetAddress<T>();
        var result = await _service.GetStateByStateRootHash(
            block.StateRootHash.ToByteArray(),
            ReservedAddresses.LegacyAccount.ToByteArray(),
            address.ToByteArray());
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
    /// <returns><see cref="IValue"/> or null if not found.</returns>
    public async Task<IValue?> GetLegacyState(Block block, Address address)
    {
        return await GetState(
            block,
            ReservedAddresses.LegacyAccount,
            address);
    }

    /// <summary>
    /// Gets the round data from specified block.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <returns><see cref="ArenaSheet.RoundData"/>round data</returns>
    public async Task<ArenaSheet.RoundData> GetRoundData(Block block)
    {
        var arenaSheet = await GetSheet<ArenaSheet>(block);
        return arenaSheet.GetRoundByBlockIndex(block.Index);
    }

    /// <summary>
    /// Retrieves the state of arena participants from the given world state and current round data.
    /// </summary>
    /// <param name="block"><see cref="Block"/>target block</param>
    /// <param name="currentRoundData"><see cref="ArenaSheet.RoundData"/>The current round data.</param>
    /// <returns>The arena participants state, or null if not found.</returns>
    public async Task<ArenaParticipants?> GetArenaParticipantsState(Block block, ArenaSheet.RoundData currentRoundData)
    {
        var participantsAddr = ArenaParticipants.DeriveAddress(
            currentRoundData.ChampionshipId,
            currentRoundData.Round);
        var participants = await GetLegacyState(block, participantsAddr) is List participantsList
            ? new ArenaParticipants(participantsList)
            : null;
        return participants;
    }

        /// <summary>
    /// Retrieves the avatar addresses and scores with ranks for a given list of avatar addresses, current round data, and world state.
    /// </summary>
    /// <param name="avatarAddrList">The list of avatar addresses.</param>
    /// <param name="currentRoundData">The current round data.</param>
    /// <param name="worldState">The world state.</param>
    /// <returns>The list of avatar addresses, scores, and ranks.</returns>
    public async Task<List<(Address avatarAddr, int score, int rank)>> AvatarAddrAndScoresWithRank(Block block, List<Address> avatarAddrList, ArenaSheet.RoundData currentRoundData)
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
        var scores = await GetStates(
            block,
            ReservedAddresses.LegacyAccount,
            avatarAndScoreAddrList.Select(tuple => tuple.Item2).ToList());
        var avatarAddrAndScores = new List<(Address avatarAddr, int score)>();
        foreach (var tuple in avatarAndScoreAddrList)
        {
            var scoreAddress = tuple.Item2;
            var score = scores[scoreAddress] is List scoreList ? (int)(Integer)scoreList[1] : ArenaScore.ArenaScoreDefault;
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
    /// Retrieve a list of arena participants based on the provided world state, avatar address list, and avatar addresses with scores and ranks.
    /// </summary>
    /// <param name="block"><see cref="Block"/> from which to retrieve the arena participants.</param>
    /// <param name="avatarAddrList">The list of avatar addresses to filter the matching participants.</param>
    /// <param name="avatarAddrAndScoresWithRank">The list of avatar addresses with their scores and ranks.</param>
    /// <returns><see cref="Task"/>A list of arena participants.</returns>
    public async Task<List<ArenaParticipant>> GetArenaParticipants(Block block, List<Address> avatarAddrList, List<(Address avatarAddr, int score, int rank)> avatarAddrAndScoresWithRank)
    {
        var runeListSheet = await GetSheet<RuneListSheet>(block);
        var costumeSheet = await GetSheet<CostumeStatSheet>(block);
        var characterSheet = await GetSheet<CharacterSheet>(block);
        var runeOptionSheet = await GetSheet<RuneOptionSheet>(block);
        var runeLevelBonusSheet = await GetSheet<RuneLevelBonusSheet>(block);
        var runeIds = runeListSheet.Values.Select(x => x.Id).ToList();
        var row = characterSheet[GameConfig.DefaultAvatarCharacterId];
        CollectionSheet collectionSheet = new CollectionSheet();
        var collectionStates = await GetCollectionStates(block, avatarAddrList);
        bool collectionSheetExist = true;
        try
        {
            collectionSheet = await GetSheet<CollectionSheet>(block);
        }
        catch (Exception)
        {
            collectionSheetExist = false;
        }

        var tasks = avatarAddrAndScoresWithRank.Select(async tuple =>
        {
            var (avatarAddr, score, rank) = tuple;
            var runeStates = await GetRuneState(block, avatarAddr, runeListSheet);
            var avatar = await GetAvatarState(block, avatarAddr);
            var itemSlotState =
                await GetLegacyState(block, ItemSlotState.DeriveAddress(avatarAddr, BattleType.Arena)) is
                    List itemSlotList
                    ? new ItemSlotState(itemSlotList)
                    : new ItemSlotState(BattleType.Arena);

            var runeSlotState =
                await GetLegacyState(block, RuneSlotState.DeriveAddress(avatarAddr, BattleType.Arena)) is
                    List runeSlotList
                    ? new RuneSlotState(runeSlotList)
                    : new RuneSlotState(BattleType.Arena);

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
            await Task.CompletedTask;
            return new ArenaParticipant(
                avatarAddr,
                score,
                rank,
                avatar,
                portraitId,
                0,
                0,
                cp
            );
        }).ToList();
        var result = await Task.WhenAll(tasks);
        return result.OrderBy(i => i.Rank).ToList();
    }

    /// <summary>
    /// Retrieves the collection states for the given addresses from the world state.
    /// </summary>
    /// <param name="block">The world state used to retrieve the collection states.</param>
    /// <param name="addresses">The list of addresses to retrieve the collection states for.</param>
    /// <returns>A dictionary of Address and CollectionState pairs representing the collection states
    /// for the given addresses,
    /// or an empty dictionary for addresses that do not have a collection state.</returns>
    public async Task<Dictionary<Address, CollectionState>> GetCollectionStates(Block block, IReadOnlyList<Address> addresses)
    {
        var result = new Dictionary<Address, CollectionState>();
        var values = await GetStates(block, Addresses.Collection, addresses);
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

    public async Task<Dictionary<Address, IValue>> GetStates(Block block, Address accountAddress, IReadOnlyList<Address> addresses)
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var queryResult = await _service.GetBulkStateByStateRootHash(block.StateRootHash.ToByteArray(), accountAddress.ToByteArray(),
            addresses.Select(i => i.ToByteArray()));
        queryResult
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(kv =>
            {
                result.TryAdd(new Address(kv.Key), _codec.Decode(kv.Value));
            });
        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public async Task<AllRuneState> GetRuneState(Block block, Address avatarAddress, RuneListSheet runeListSheet)
    {
        var serialized = await GetState(block, Addresses.RuneState, avatarAddress);
        AllRuneState allRuneState;
        if (serialized is null)
        {
            // Get legacy rune states
            allRuneState = new AllRuneState();
            foreach (var rune in runeListSheet.Values)
            {
                var runeAddress = RuneState.DeriveAddress(avatarAddress, rune.Id);
                if (await GetLegacyState(block, runeAddress) is List rawState)
                {
                    var runeState = new RuneState(rawState);
                    allRuneState.AddRuneState(runeState);
                }
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
    /// <returns><see cref="IValue"/> or null if not found.</returns>
    public async Task<IValue?> GetState(Block block, Address accountAddress, Address address)
    {
        byte[] result = [];
        var retry = 0;
        while (retry < 3)
        {
            try
            {
                result = await _service.GetStateByStateRootHash(
                    block.StateRootHash.ToByteArray(),
                    accountAddress.ToByteArray(),
                    address.ToByteArray());
                break;
            }
            catch (RpcException)
            {
                await Task.Delay((3 - retry) * 1000);
                retry++;
            }
        }
        if (_codec.Decode(result) is { } decode and not Null)
        {
            return decode;
        }

        return null;
    }

    public async Task<Inventory> GetInventory(Block block, Address avatarAddress)
    {
        var inventoryResult = await GetState(block, Addresses.Inventory, avatarAddress);
        if (inventoryResult is List l)
        {
            return new Inventory(l);
        }

        return new Inventory();
    }

    public async Task<AvatarState> GetAvatarState(Block block, Address avatarAddress)
    {
        var avatarResult = await GetState(block, Addresses.Avatar, avatarAddress);
        if (avatarResult is List l)
        {
            var avatarState = new AvatarState(l);
            var inventory = await GetInventory(block, avatarAddress);
            avatarState.inventory = inventory;
            return avatarState;
        }

        throw new Exception();
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

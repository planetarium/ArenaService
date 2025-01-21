using ArenaService.Client;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Options;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Options;

namespace ArenaService.Worker;

public class BattleProcessor
{
    private readonly Address BattleAccountAddress = new("0000000000000000000000000000000000000027");
    private static readonly Codec Codec = new();
    private readonly string _arenaProviderName;
    private readonly ILogger<BattleProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IBattleRepository _battleRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ITxTrackingService _txTrackingService;

    public BattleProcessor(
        ILogger<BattleProcessor> logger,
        IHeadlessClient client,
        IBattleRepository battleRepo,
        IRankingRepository rankingRepo,
        ITxTrackingService txTrackingService,
        IParticipantRepository participantRepo,
        IOptions<OpsConfigOptions> options
    )
    {
        _logger = logger;
        _client = client;
        _battleRepo = battleRepo;
        _rankingRepo = rankingRepo;
        _txTrackingService = txTrackingService;
        _participantRepo = participantRepo;
        _arenaProviderName = options.Value.ArenaProviderName;
    }

    public async Task ProcessAsync(TxId txId, int battleId)
    {
        var battle = await _battleRepo.GetBattleAsync(battleId);
        if (battle is null)
        {
            _logger.LogError($"Battle log with ID {battleId} not found.");
            return;
        }

        await _battleRepo.UpdateTxIdAsync(battleId, txId);

        await _txTrackingService.TrackTransactionAsync(
            txId,
            async status =>
            {
                await _battleRepo.UpdateTxStatusAsync(battleId, status.ToModelTxStatus());
            },
            async successResponse =>
            {
                var isVictory = await GetBattleResultState(battle, txId);
                await UpdateData(battle, isVictory);
                _logger.LogInformation($"Tx succeeded!");
            },
            txId =>
            {
                throw new TimeoutException($"Transaction timed out for ID: {txId}");
            }
        );
    }

    private async Task<bool> GetBattleResultState(Battle battle, TxId txId)
    {
        var accountAddress = BattleAccountAddress.Derive(_arenaProviderName);
        var stateAddress = battle.AvailableOpponent.AvatarAddress.Derive(txId.ToString());

        var state = await RetryUtility.RetryAsync(
            async () =>
            {
                var stateResponse = await _client.GetState.ExecuteAsync(
                    accountAddress.ToHex(),
                    stateAddress.ToHex()
                );

                if (stateResponse.Data?.State is null)
                {
                    return null;
                }

                return Codec.Decode(Convert.FromHexString(stateResponse.Data.State));
            },
            maxAttempts: 5,
            delayMilliseconds: 2000,
            successCondition: state => state != null,
            onRetry: attempt =>
            {
                _logger.LogDebug($"Retry attempt {attempt}: State is null, retrying...");
            }
        );

        var isVictory = (Integer)state == 1;

        return isVictory;
    }

    private async Task UpdateData(Battle battle, bool isVictory)
    {
        var myScoreChange = isVictory ? 10 : -10;
        var enemyScoreChange = isVictory ? -10 : 10;

        await _battleRepo.UpdateBattleResultAsync(
            battle.Id,
            isVictory,
            myScoreChange,
            enemyScoreChange,
            1
        );
        // var attackerAddress = new Address(battle.Attacker.AvatarAddress);
        // var defenderAddress = new Address(battle.Defender.AvatarAddress);

        // await _participantRepo.UpdateScoreAsync(battle.SeasonId, attackerAddress, myScoreChange);
        // await _participantRepo.UpdateScoreAsync(
        //     battle.SeasonId,
        //     defenderAddress,
        //     enemyScoreChange
        // );
        // await _rankingRepo.UpdateScoreAsync(attackerAddress, battle.SeasonId, myScoreChange);
        // await _rankingRepo.UpdateScoreAsync(defenderAddress, battle.SeasonId, enemyScoreChange);
    }
}

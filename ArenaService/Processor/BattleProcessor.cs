using ArenaService.Client;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Arena;

namespace ArenaService.Worker;

public class BattleProcessor
{
    private static readonly Codec Codec = new();
    private readonly ILogger<BattleProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IBattleLogRepository _battleLogRepo;
    private readonly IRankingRepository _rankingRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ITxTrackingService _txTrackingService;

    public BattleProcessor(
        ILogger<BattleProcessor> logger,
        IHeadlessClient client,
        IBattleLogRepository battleLogRepo,
        IRankingRepository rankingRepo,
        ITxTrackingService txTrackingService,
        IParticipantRepository participantRepo
    )
    {
        _logger = logger;
        _client = client;
        _battleLogRepo = battleLogRepo;
        _rankingRepo = rankingRepo;
        _txTrackingService = txTrackingService;
        _participantRepo = participantRepo;
    }

    public async Task ProcessAsync(TxId txId, int battleLogId)
    {
        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);
        if (battleLog is null)
        {
            _logger.LogError($"Battle log with ID {battleLogId} not found.");
            return;
        }

        await _battleLogRepo.UpdateTxIdAsync(battleLogId, txId.ToString());

        await _txTrackingService.TrackTransactionAsync(
            txId,
            async status =>
            {
                await _battleLogRepo.UpdateTxStatusAsync(battleLogId, status.ToModelTxStatus());
            },
            async successResponse =>
            {
                var isVictory = await GetBattleResultState(battleLog, txId);
                await UpdateData(battleLog, isVictory);
                _logger.LogInformation($"Tx succeeded!");
            },
            txId =>
            {
                throw new TimeoutException($"Transaction timed out for ID: {txId}");
            }
        );
    }

    private async Task<bool> GetBattleResultState(BattleLog battleLog, TxId txId)
    {
        var accountAddress = Addresses.Battle.Derive(ArenaProvider.PLANETARIUM.ToString());
        var stateAddress = new Address(battleLog.Attacker.AvatarAddress).Derive(txId.ToString());

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

    private async Task UpdateData(BattleLog battleLog, bool isVictory)
    {
        var myScoreChange = isVictory ? 10 : -10;
        var enemyScoreChange = isVictory ? -10 : 10;

        await _battleLogRepo.UpdateBattleResultAsync(
            battleLog.Id,
            isVictory,
            myScoreChange,
            enemyScoreChange,
            1
        );
        var attackerAddress = new Address(battleLog.Attacker.AvatarAddress);
        var defenderAddress = new Address(battleLog.Defender.AvatarAddress);

        await _participantRepo.UpdateScoreAsync(battleLog.SeasonId, attackerAddress, myScoreChange);
        await _participantRepo.UpdateScoreAsync(
            battleLog.SeasonId,
            defenderAddress,
            enemyScoreChange
        );
        await _rankingRepo.UpdateScoreAsync(attackerAddress, battleLog.SeasonId, myScoreChange);
        await _rankingRepo.UpdateScoreAsync(defenderAddress, battleLog.SeasonId, enemyScoreChange);
    }
}

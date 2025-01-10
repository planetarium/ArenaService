using ArenaService.Client;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Arena;

namespace ArenaService.Worker;

public class BattleTaskProcessor
{
    private static readonly Codec Codec = new();
    private readonly ILogger<BattleTaskProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IBattleLogRepository _battleLogRepo;
    private readonly IParticipantRepository _participantRepo;

    public BattleTaskProcessor(
        ILogger<BattleTaskProcessor> logger,
        IHeadlessClient client,
        IBattleLogRepository battleLogRepo,
        IParticipantRepository participantRepo
    )
    {
        _logger = logger;
        _client = client;
        _battleLogRepo = battleLogRepo;
        _participantRepo = participantRepo;
    }

    public async Task ProcessAsync(string txId, int battleLogId)
    {
        _logger.LogInformation($"Watch the battle: {txId}, {battleLogId}");
        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);
        await _battleLogRepo.UpdateTxIdAsync(battleLogId, txId);

        for (int i = 0; i < 30; i++)
        {
            var txResultResponse = await _client.GetTransactionResult.ExecuteAsync(txId);

            if (txResultResponse.Data is null)
            {
                _logger.LogInformation($"TxResult is null");
                await Task.Delay(1000);
                continue;
            }
            await _battleLogRepo.UpdateTxStatusAsync(
                battleLogId,
                txResultResponse.Data.Transaction.TransactionResult.TxStatus.ToModelTxStatus()
            );

            switch (txResultResponse.Data.Transaction.TransactionResult.TxStatus)
            {
                case TxStatus.Success:
                    if (battleLog is null)
                    {
                        _logger.LogInformation($"battleLog is null");
                        return;
                    }

                    var accountAddress = Addresses.Battle.Derive(
                        ArenaProvider.PLANETARIUM.ToString()
                    );
                    var stateAddress = new Address(battleLog.Attacker.AvatarAddress).Derive(
                        txId.ToString()
                    );

                    var stateResponse = await _client.GetState.ExecuteAsync(
                        accountAddress.ToHex(),
                        stateAddress.ToHex()
                    );

                    if (stateResponse.Data is null || stateResponse.Data.State is null)
                    {
                        _logger.LogInformation($"state is null");
                        return;
                    }

                    var state = Codec.Decode(Convert.FromHexString(stateResponse.Data.State));
                    var IsVictory = (Integer)state == 1;
                    var myScoreChange = IsVictory ? 10 : -10;
                    var enemyScoreChange = IsVictory ? -10 : 10;

                    await _battleLogRepo.UpdateBattleResultAsync(
                        battleLogId,
                        IsVictory,
                        myScoreChange,
                        enemyScoreChange,
                        1
                    );
                    await _participantRepo.UpdateScoreAsync(
                        battleLog.SeasonId,
                        new Address(battleLog.Attacker.AvatarAddress),
                        myScoreChange
                    );
                    await _participantRepo.UpdateScoreAsync(
                        battleLog.SeasonId,
                        new Address(battleLog.Defender.AvatarAddress),
                        enemyScoreChange
                    );
                    return;
                default:
                    _logger.LogInformation(
                        $"TxResult is {txResultResponse.Data.Transaction.TransactionResult.TxStatus}"
                    );
                    break;
            }

            await Task.Delay(1000);
        }
    }
}

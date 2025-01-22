using ArenaService.ActionValues;
using ArenaService.Client;
using ArenaService.Constants;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Models.BattleTicket;
using ArenaService.Models.Enums;
using ArenaService.Options;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
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
    private readonly IMedalRepository _medalRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITxTrackingService _txTrackingService;

    public BattleProcessor(
        ILogger<BattleProcessor> logger,
        IHeadlessClient client,
        IBattleRepository battleRepo,
        IRankingRepository rankingRepo,
        IMedalRepository medalRepo,
        IAvailableOpponentRepository availableOpponentRepo,
        ITicketRepository ticketRepo,
        ITxTrackingService txTrackingService,
        IParticipantRepository participantRepo,
        IOptions<OpsConfigOptions> options
    )
    {
        _logger = logger;
        _client = client;
        _battleRepo = battleRepo;
        _rankingRepo = rankingRepo;
        _ticketRepo = ticketRepo;
        _medalRepo = medalRepo;
        _txTrackingService = txTrackingService;
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _arenaProviderName = options.Value.ArenaProviderName;
    }

    public async Task<string> ProcessAsync(int battleId)
    {
        var battle = await _battleRepo.GetBattleAsync(
            battleId,
            q =>
                q.Include(b => b.AvailableOpponent)
                    .ThenInclude(ao => ao.Opponent)
                    .ThenInclude(p => p.User)
                    .Include(b => b.Season)
                    .ThenInclude(s => s.BattleTicketPolicy)
                    .Include(b => b.Participant)
        );
        if (battle is null)
        {
            return $"Battle log with ID {battleId} not found.";
        }
        if (battle.TxId is null)
        {
            return $"Battle log {battleId} doesn't have any tx.";
        }

        if (!await ValidateUsedTxId(battle.TxId.Value, battleId))
        {
            await _battleRepo.UpdateBattle(
                battle,
                btpl =>
                {
                    btpl.BattleStatus = BattleStatus.DUPLICATE_TRANSACTION;
                }
            );
            return $"{battle.TxId} is used tx";
        }

        var battleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            battle.RoundId,
            battle.AvatarAddress
        );
        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            battle.SeasonId,
            battle.AvatarAddress
        );

        if (battleTicketStatusPerRound is null || battleTicketStatusPerSeason is null)
        {
            (battleTicketStatusPerRound, battleTicketStatusPerSeason) = await AddTicketStatuses(
                battle
            );
        }

        if (battleTicketStatusPerRound.RemainingCount <= 0)
        {
            return $"Doesn't have remaining ticket";
        }

        if (!await ValidateUsedTxId(battle.TxId.Value, battleId))
        {
            await _battleRepo.UpdateBattle(
                battle,
                btpl =>
                {
                    btpl.BattleStatus = BattleStatus.DUPLICATE_TRANSACTION;
                }
            );
            return $"{battle.TxId} is used tx";
        }

        var processResult = "before tracking";

        await _battleRepo.UpdateBattle(
            battle,
            b =>
            {
                b.BattleStatus = BattleStatus.TRACKING;
            }
        );

        await _txTrackingService.TrackTransactionAsync(
            battle.TxId.Value,
            async status =>
            {
                if (status == Client.TxStatus.Failure)
                {
                    await _battleRepo.UpdateBattle(
                        battle,
                        b =>
                        {
                            b.TxStatus = status.ToModelTxStatus();
                            b.BattleStatus = BattleStatus.TX_FAILED;
                        }
                    );
                    processResult = "tx failed";
                }
                else
                {
                    await _battleRepo.UpdateBattle(
                        battle,
                        b =>
                        {
                            b.TxStatus = status.ToModelTxStatus();
                        }
                    );
                }
            },
            async successResponse =>
            {
                var battleActionValue = await FetchAndSearchBattleAction(battle.TxId.Value);

                if (battleActionValue is null)
                {
                    await _battleRepo.UpdateBattle(
                        battle,
                        b =>
                        {
                            b.BattleStatus = BattleStatus.NOT_FOUND_BATTLE_ACTION;
                        }
                    );
                    processResult = $"Not found tx for {battle.TxId}";
                }
                else
                {
                    if (!ValidateToken(battleActionValue))
                    {
                        await _battleRepo.UpdateBattle(
                            battle,
                            b =>
                            {
                                b.BattleStatus = BattleStatus.INVALID_BATTLE;
                            }
                        );
                        processResult = "invalid battle";
                    }
                    else
                    {
                        var isVictory = await GetBattleResultState(battle, battle.TxId.Value);
                        await UpdateModels(
                            battle,
                            battleTicketStatusPerRound,
                            battleTicketStatusPerSeason,
                            isVictory
                        );
                        processResult = "success";
                    }
                }
            },
            txId =>
            {
                throw new TimeoutException($"Transaction timed out for ID: {txId}");
            }
        );

        return processResult;
    }

    private async Task<(BattleTicketStatusPerRound, BattleTicketStatusPerSeason)> AddTicketStatuses(
        Battle battle
    )
    {
        var battleTicketStatusPerRound = await _ticketRepo.AddBattleTicketStatusPerRound(
            battle.SeasonId,
            battle.RoundId,
            battle.AvatarAddress,
            battle.Season.BattleTicketPolicyId,
            battle.Season.BattleTicketPolicy.DefaultTicketsPerRound,
            0,
            0
        );
        var battleTicketStatusPerSeason = await _ticketRepo.AddBattleTicketStatusPerSeason(
            battle.SeasonId,
            battle.AvatarAddress,
            battle.Season.BattleTicketPolicyId,
            0,
            0
        );

        return (battleTicketStatusPerRound, battleTicketStatusPerSeason);
    }

    private async Task<bool> ValidateUsedTxId(TxId txId, int battleId)
    {
        var battle = await _battleRepo.GetBattleByTxId(txId, battleId);
        return battle is null;
    }

    private async Task<BattleActionValue?> FetchAndSearchBattleAction(TxId txId)
    {
        var txResponse = await RetryUtility.RetryAsync(
            async () =>
            {
                var txResponse = await _client.GetTx.ExecuteAsync(txId.ToString());

                if (txResponse.Data is null || txResponse.Data!.Transaction.GetTx is null)
                {
                    _logger.LogInformation("Transaction result is null. Retrying...");
                    return null;
                }

                return txResponse;
            },
            maxAttempts: 5,
            delayMilliseconds: 2000,
            successCondition: txResponse => txResponse != null,
            onRetry: attempt =>
            {
                _logger.LogDebug($"Retry attempt {attempt}: txResponse is null, retrying...");
            }
        );

        foreach (var actionResponse in txResponse!.Data!.Transaction.GetTx!.Actions)
        {
            var action = Codec.Decode(Convert.FromHexString(actionResponse!.Raw));

            if (BattleActionParser.TryParseActionPayload(action, out var battleActionValue))
            {
                return battleActionValue;
            }
            else
            {
                continue;
            }
        }

        return null;
    }

    private bool ValidateToken(BattleActionValue battleActionValue)
    {
        return true;
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

    private async Task UpdateModels(
        Battle battle,
        BattleTicketStatusPerRound battleTicketStatusPerRound,
        BattleTicketStatusPerSeason battleTicketStatusPerSeason,
        bool isVictory
    )
    {
        var scoreDict = OpponentGroupConstants.Groups[battle.AvailableOpponent.GroupId];

        var myScoreChange = isVictory ? scoreDict.WinScore : scoreDict.LoseScore;
        var opponentScoreChange = isVictory ? -1 : 0;

        await _battleRepo.UpdateBattle(
            battle,
            b =>
            {
                b.IsVictory = isVictory;
                b.BattleStatus = BattleStatus.SUCCESS;
                b.MyScoreChange = myScoreChange;
                b.OpponentScoreChange = opponentScoreChange;
            }
        );
        await _participantRepo.UpdateParticipantAsync(
            battle.Participant,
            p =>
            {
                p.Score += myScoreChange;
            }
        );
        await _participantRepo.UpdateParticipantAsync(
            battle.AvailableOpponent.Opponent,
            p =>
            {
                p.Score += opponentScoreChange;
            }
        );
        await _rankingRepo.UpdateScoreAsync(
            battle.AvatarAddress,
            battle.SeasonId,
            battle.RoundId + 1,
            myScoreChange
        );
        await _rankingRepo.UpdateScoreAsync(
            battle.AvailableOpponent.AvatarAddress,
            battle.SeasonId,
            battle.RoundId + 1,
            opponentScoreChange
        );

        await _ticketRepo.UpdateBattleTicketStatusPerRound(
            battleTicketStatusPerRound,
            rts =>
            {
                rts.RemainingCount -= 1;
                rts.UsedCount += 1;
            }
        );
        await _ticketRepo.UpdateBattleTicketStatusPerSeason(
            battleTicketStatusPerSeason,
            rts =>
            {
                rts.UsedCount += 1;
            }
        );

        await _ticketRepo.AddBattleTicketUsageLog(
            battleTicketStatusPerRound.Id,
            battleTicketStatusPerSeason.Id,
            battle.Id
        );

        await _availableOpponentRepo.UpdateAvailableOpponent(
            battle.AvailableOpponent,
            ao =>
            {
                ao.SuccessBattleId = battle.Id;
            }
        );
        if (battle.Season.ArenaType == ArenaType.SEASON && isVictory)
        {
            var medal = await _medalRepo.GetMedalAsync(battle.SeasonId, battle.AvatarAddress);
            if (medal is null)
            {
                await _medalRepo.AddMedalAsync(battle.SeasonId, battle.AvatarAddress);
            }
            else
            {
                await _medalRepo.UpdateMedalAsync(medal, m => m.MedalCount += 1);
            }
        }
    }
}

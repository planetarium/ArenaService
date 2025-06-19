using System.Data;
using ArenaService.ActionValues;
using ArenaService.Client;
using ArenaService.Extensions;
using ArenaService.Options;
using ArenaService.Services;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Data;
using ArenaService.Shared.Extensions;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ArenaService.Worker;

public class BattleProcessor
{
    private readonly Address BattleAccountAddress = new("0000000000000000000000000000000000000027");
    private static readonly Codec Codec = new();
    private readonly string _arenaProviderName;
    private readonly ILogger<BattleProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IBattleRepository _battleRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRankingService _rankingService;
    private readonly IMedalRepository _medalRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ITxTrackingService _txTrackingService;
    private readonly BattleTokenValidator _battleTokenValidator;
    private readonly ArenaDbContext _dbContext;

    public BattleProcessor(
        ILogger<BattleProcessor> logger,
        IHeadlessClient client,
        IBattleRepository battleRepo,
        IUserRepository userRepo,
        IRankingService rankingService,
        IMedalRepository medalRepo,
        IAvailableOpponentRepository availableOpponentRepo,
        ITicketRepository ticketRepo,
        ITxTrackingService txTrackingService,
        IParticipantRepository participantRepo,
        IOptions<OpsConfigOptions> options,
        ArenaDbContext dbContext
    )
    {
        _logger = logger;
        _client = client;
        _battleRepo = battleRepo;
        _userRepo = userRepo;
        _rankingService = rankingService;
        _ticketRepo = ticketRepo;
        _medalRepo = medalRepo;
        _txTrackingService = txTrackingService;
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _arenaProviderName = options.Value.ArenaProviderName;
        _battleTokenValidator = new BattleTokenValidator(options.Value.JwtPublicKey);
        _dbContext = dbContext;
    }

    public async Task<string> ProcessAsync(int battleId)
    {
        var battle = await _battleRepo.GetBattleAsync(
            battleId,
            q =>
                q.Include(b => b.AvailableOpponent)
                    .ThenInclude(ao => ao.Opponent)
                    .ThenInclude(p => p.User)
                    .ThenInclude(u => u.Clan)
                    .Include(b => b.Season)
                    .ThenInclude(s => s.BattleTicketPolicy)
                    .Include(b => b.Round)
                    .Include(b => b.Participant)
                    .ThenInclude(p => p.User)
                    .ThenInclude(u => u.Clan)
        );
        if (battle is null)
        {
            return $"Battle log with ID {battleId} not found.";
        }
        if (battle.TxId is null)
        {
            return $"Battle log {battleId} doesn't have any tx.";
        }
        if (battle.AvailableOpponent.SuccessBattleId is not null)
        {
            return $"Already have success battle id";
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
                await _battleRepo.UpdateBattle(
                    battle,
                    b =>
                    {
                        b.TxStatus = status.ToModelTxStatus();
                    }
                );
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
                    if (!ValidateToken(battle, battleActionValue))
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
                        var battleResult = await GetBattleResultState(battle, battle.TxId.Value);
                        processResult = await UpdateModels(
                            battle,
                            battleTicketStatusPerRound,
                            battleTicketStatusPerSeason,
                            battleResult
                        );
                    }
                }
            },
            async failureResponse =>
            {
                await _battleRepo.UpdateBattle(
                    battle,
                    b =>
                    {
                        b.TxStatus = Shared.Models.Enums.TxStatus.FAILURE;
                        b.BattleStatus = BattleStatus.TX_FAILED;
                        b.ExceptionNames = failureResponse.ExceptionNames is not null
                            ? string.Join(", ", failureResponse.ExceptionNames)
                            : null;
                    }
                );
                processResult = "tx failed";
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

    private bool ValidateToken(Battle battle, BattleActionValue battleActionValue)
    {
        return battleActionValue.ArenaProvider == _arenaProviderName
            && _battleTokenValidator.ValidateBattleToken(battleActionValue.Memo, battle.Id);
    }

    private async Task<BattleResultState> GetBattleResultState(Battle battle, TxId txId)
    {
        var accountAddress = BattleAccountAddress.Derive(_arenaProviderName);
        var stateAddress = battle.AvailableOpponent.AvatarAddress.Derive(txId.ToString());

        var state = await RetryUtility.RetryAsync(
            async () =>
            {
                var stateResponse = await _client.GetState.ExecuteAsync(
                    accountAddress.ToHex().ToLower(),
                    stateAddress.ToHex().ToLower()
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
        var battleResult = new BattleResultState(state!);

        return battleResult;
    }

    private async Task<string> UpdateModels(
        Battle battle,
        BattleTicketStatusPerRound battleTicketStatusPerRound,
        BattleTicketStatusPerSeason battleTicketStatusPerSeason,
        BattleResultState battleResult
    )
    {
        var scoreDict = OpponentGroupConstants.Groups[battle.AvailableOpponent.GroupId];
        var myScoreChange = battleResult.IsVictory ? scoreDict.WinScore : scoreDict.LoseScore;
        var opponentScoreChange = battleResult.IsVictory ? -1 : 0;

        using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var updated = await _availableOpponentRepo.TrySetSuccessBattleId(
                battle.AvailableOpponent.Id,
                battle.Id
            );

            if (!updated)
            {
                await transaction.RollbackAsync();
                return "Already have success battle id";
            }

            var deductResult = await _ticketRepo.DeductBattleTicket(
                battleTicketStatusPerRound.Id,
                battleTicketStatusPerSeason.Id,
                battleResult.IsVictory
            );

            if (!deductResult)
            {
                await _battleRepo.UpdateBattle(
                    battle,
                    b => b.BattleStatus = BattleStatus.NO_REMAINING_TICKET
                );
                await transaction.CommitAsync();
                return "No remaining battle tickets";
            }

            await _battleRepo.UpdateBattle(
                battle,
                b =>
                {
                    b.IsVictory = battleResult.IsVictory;
                    b.BattleStatus = BattleStatus.SUCCESS;
                    b.MyScoreChange = myScoreChange;
                    b.OpponentScoreChange = opponentScoreChange;
                }
            );
            await _rankingService.UpdateScoreAsync(
                battle.AvatarAddress,
                battle.SeasonId,
                battle.Round.RoundIndex + 1,
                myScoreChange,
                battle.Participant.User.Clan is null ? null : battle.Participant.User.Clan.Id
            );
            if (opponentScoreChange != 0)
            {
                await _rankingService.UpdateScoreAsync(
                    battle.AvailableOpponent.OpponentAvatarAddress,
                    battle.SeasonId,
                    battle.Round.RoundIndex + 1,
                    opponentScoreChange,
                    battle.AvailableOpponent.Opponent.User.Clan is null
                        ? null
                        : battle.AvailableOpponent.Opponent.User.Clan.Id
                );
            }
            await _participantRepo.UpdateMyScore(
                battle.SeasonId,
                battle.AvatarAddress,
                myScoreChange,
                battleResult.IsVictory
            );
            await _participantRepo.UpdateOpponentScore(
                battle.SeasonId,
                battle.AvailableOpponent.OpponentAvatarAddress,
                opponentScoreChange
            );

            await _ticketRepo.AddBattleTicketUsageLog(
                battleTicketStatusPerRound.Id,
                battleTicketStatusPerSeason.Id,
                battle.Id
            );

            await _userRepo.UpdateUserAsync(
                battle.Participant.User,
                u =>
                {
                    u.PortraitId = battleResult.PortraitId;
                    u.Cp = battleResult.Cp;
                    u.Level = battleResult.Level;
                }
            );

            if (battle.Season.ArenaType == ArenaType.SEASON && battleResult.IsVictory)
            {
                await _medalRepo.AddOrUpdateMedal(battle.SeasonId, battle.AvatarAddress);
            }

            await transaction.CommitAsync();
            return "success";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

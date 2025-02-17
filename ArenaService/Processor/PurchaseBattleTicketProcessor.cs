using System.Globalization;
using System.Text.RegularExpressions;
using ArenaService.ActionValues;
using ArenaService.Client;
using ArenaService.Extensions;
using ArenaService.Options;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArenaService.Worker;

public class PurchaseBattleTicketProcessor
{
    private readonly Address _recipientAddress;
    private readonly Codec Codec = new();
    private readonly ILogger<PurchaseBattleTicketProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly ITxTrackingService _txTrackingService;

    public PurchaseBattleTicketProcessor(
        ILogger<PurchaseBattleTicketProcessor> logger,
        IHeadlessClient client,
        ITicketRepository ticketRepo,
        ISeasonRepository seasonRepo,
        ITxTrackingService txTrackingService,
        IOptions<OpsConfigOptions> options
    )
    {
        _logger = logger;
        _client = client;
        _ticketRepo = ticketRepo;
        _seasonRepo = seasonRepo;
        _txTrackingService = txTrackingService;
        _recipientAddress = new Address(options.Value.RecipientAddress);
    }

    public async Task<string> ProcessAsync(int purchaseLogId)
    {
        string processResult = "before track";

        var purchaseLog = await _ticketRepo.GetBattleTicketPurchaseLogById(purchaseLogId);

        if (purchaseLog is null)
        {
            return $"Not found purchase log {purchaseLogId}";
        }

        if (!await ValidateUsedTxId(purchaseLog.TxId, purchaseLogId))
        {
            await _ticketRepo.UpdateBattleTicketPurchaseLog(
                purchaseLog,
                btpl =>
                {
                    btpl.PurchaseStatus = PurchaseStatus.DUPLICATE_TRANSACTION;
                }
            );
            return $"{purchaseLog.TxId} is used tx";
        }

        await _ticketRepo.UpdateBattleTicketPurchaseLog(
            purchaseLog,
            btpl =>
            {
                btpl.PurchaseStatus = PurchaseStatus.TRACKING;
            }
        );

        var season = await _seasonRepo.GetSeasonAsync(
            purchaseLog.SeasonId,
            q => q.Include(s => s.BattleTicketPolicy)
        );

        await _txTrackingService.TrackTransactionAsync(
            purchaseLog.TxId,
            async status =>
            {
                await _ticketRepo.UpdateBattleTicketPurchaseLog(
                    purchaseLog,
                    btpl =>
                    {
                        btpl.TxStatus = status.ToModelTxStatus();
                    }
                );
            },
            async successResponse =>
            {
                var taActionValue = await FetchAndSearchTransferAssetAction(purchaseLog.TxId);

                if (taActionValue is null)
                {
                    await _ticketRepo.UpdateBattleTicketPurchaseLog(
                        purchaseLog,
                        btpl =>
                        {
                            btpl.PurchaseStatus = PurchaseStatus.NOT_FOUND_TRANSFER_ASSETS_ACTION;
                        }
                    );
                    processResult = $"Not found tx for {purchaseLog.TxId}";
                }
                else
                {
                    if (taActionValue!.Recipient != _recipientAddress)
                    {
                        await _ticketRepo.UpdateBattleTicketPurchaseLog(
                            purchaseLog,
                            btpl =>
                            {
                                btpl.PurchaseStatus = PurchaseStatus.INVALID_RECIPIENT;
                            }
                        );
                        processResult = $"{_recipientAddress} is not ops address";
                    }
                    else
                    {
                        var requiredAmount = await CalcRequiredAmount(
                            season,
                            purchaseLog.AvatarAddress,
                            purchaseLog.PurchaseCount
                        );

                        if (!ValidateCostPaid(taActionValue, requiredAmount))
                        {
                            await _ticketRepo.UpdateBattleTicketPurchaseLog(
                                purchaseLog,
                                btpl =>
                                {
                                    btpl.PurchaseStatus = PurchaseStatus.INSUFFICIENT_PAYMENT;
                                }
                            );

                            processResult =
                                $"Insufficient payment. paid: {taActionValue.Amount} required: {requiredAmount}";
                        }
                        else
                        {
                            await UpdateTicket(season, purchaseLog);
                            await _ticketRepo.UpdateBattleTicketPurchaseLog(
                                purchaseLog,
                                btpl =>
                                {
                                    btpl.PurchaseStatus = PurchaseStatus.SUCCESS;
                                    btpl.AmountPaid = requiredAmount;
                                }
                            );

                            processResult = "success";
                        }
                    }
                }
            },
            async failureResponse =>
            {
                await _ticketRepo.UpdateBattleTicketPurchaseLog(
                    purchaseLog,
                    btpl =>
                    {
                        btpl.TxStatus = Shared.Models.Enums.TxStatus.FAILURE;
                        btpl.PurchaseStatus = PurchaseStatus.TX_FAILED;
                        btpl.ExceptionNames = failureResponse.ExceptionNames?.ToString();
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

    private async Task<TransferAssetsActionValue?> FetchAndSearchTransferAssetAction(TxId txId)
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

            if (TransferAssetsActionParser.TryParseActionPayload(action, out var taActionValue))
            {
                return taActionValue;
            }
            else
            {
                continue;
            }
        }

        return null;
    }

    private bool ValidateCostPaid(TransferAssetsActionValue taActionValue, decimal requiredAmount)
    {
        if (taActionValue.Amount.Currency.Ticker == "NCG")
        {
            decimal calculatedAmount = decimal.Parse(
                taActionValue.Amount.GetQuantityString(),
                CultureInfo.InvariantCulture
            );

            if (Math.Abs(calculatedAmount - requiredAmount) < 0.0001m)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateUsedTxId(TxId txId, int purchaseLogId)
    {
        var battleTicketPurchaseLog = await _ticketRepo.GetBattleTicketPurchaseLogByTxId(
            txId,
            purchaseLogId
        );
        var refreshTicketPurchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogByTxId(
            txId,
            null
        );
        return battleTicketPurchaseLog is null && refreshTicketPurchaseLog is null;
    }

    private async Task<decimal> CalcRequiredAmount(
        Season season,
        Address avatarAddress,
        int purchaseCount
    )
    {
        var requiredAmount = 0m;

        var battleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            season.Id,
            avatarAddress
        );
        var purchasedCount = battleTicketStatusPerSeason is null
            ? 0
            : battleTicketStatusPerSeason.PurchaseCount;

        for (int i = 0; i < purchaseCount; i++)
        {
            requiredAmount += season.BattleTicketPolicy.GetPrice(purchasedCount + i);
        }

        return requiredAmount;
    }

    private async Task UpdateTicket(Season season, BattleTicketPurchaseLog purchaseLog)
    {
        var existBattleTicketStatusPerRound = await _ticketRepo.GetBattleTicketStatusPerRound(
            purchaseLog.RoundId,
            purchaseLog.AvatarAddress
        );

        if (existBattleTicketStatusPerRound is null)
        {
            await _ticketRepo.AddBattleTicketStatusPerRound(
                purchaseLog.SeasonId,
                purchaseLog.RoundId,
                purchaseLog.AvatarAddress,
                season.BattleTicketPolicyId,
                season.BattleTicketPolicy.DefaultTicketsPerRound + purchaseLog.PurchaseCount,
                0,
                purchaseLog.PurchaseCount
            );
        }
        else
        {
            await _ticketRepo.UpdateBattleTicketStatusPerRound(
                purchaseLog.RoundId,
                purchaseLog.AvatarAddress,
                bts =>
                {
                    bts.RemainingCount += purchaseLog.PurchaseCount;
                    bts.PurchaseCount += purchaseLog.PurchaseCount;
                }
            );
        }

        var existBattleTicketStatusPerSeason = await _ticketRepo.GetBattleTicketStatusPerSeason(
            purchaseLog.SeasonId,
            purchaseLog.AvatarAddress
        );

        if (existBattleTicketStatusPerSeason is null)
        {
            await _ticketRepo.AddBattleTicketStatusPerSeason(
                purchaseLog.SeasonId,
                purchaseLog.AvatarAddress,
                season.BattleTicketPolicyId,
                0,
                purchaseLog.PurchaseCount
            );
        }
        else
        {
            await _ticketRepo.UpdateBattleTicketStatusPerSeason(
                purchaseLog.SeasonId,
                purchaseLog.AvatarAddress,
                bts =>
                {
                    bts.PurchaseCount += purchaseLog.PurchaseCount;
                }
            );
        }
    }
}

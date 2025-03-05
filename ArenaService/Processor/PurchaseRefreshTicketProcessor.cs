using System.Globalization;
using System.Text.RegularExpressions;
using ArenaService.ActionValues;
using ArenaService.Client;
using ArenaService.Shared.Extensions;
using ArenaService.Options;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Utils;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ArenaService.Extensions;
using ArenaService.Services;
using ArenaService.Shared.Data;
using System.Data;

namespace ArenaService.Worker;

public class PurchaseRefreshTicketProcessor
{
    private readonly Address _recipientAddress;
    private readonly Codec Codec = new();
    private readonly ILogger<PurchaseRefreshTicketProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeasonRepository _seasonRepo;
    private readonly ITxTrackingService _txTrackingService;
    private readonly ArenaDbContext _dbContext;

    public PurchaseRefreshTicketProcessor(
        ILogger<PurchaseRefreshTicketProcessor> logger,
        IHeadlessClient client,
        ITicketRepository ticketRepo,
        ISeasonRepository seasonRepo,
        ITxTrackingService txTrackingService,
        IOptions<OpsConfigOptions> options,
        ArenaDbContext dbContext
    )
    {
        _logger = logger;
        _client = client;
        _ticketRepo = ticketRepo;
        _seasonRepo = seasonRepo;
        _txTrackingService = txTrackingService;
        _recipientAddress = new Address(options.Value.RecipientAddress);
        _dbContext = dbContext;
    }

    public async Task<string> ProcessAsync(int purchaseLogId)
    {
        string processResult = "before track";

        var purchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogById(purchaseLogId);

        if (purchaseLog is null)
        {
            return $"Not found purchase log {purchaseLogId}";
        }

        var season = await _seasonRepo.GetSeasonAsync(
            purchaseLog.SeasonId,
            q => q.Include(s => s.RefreshTicketPolicy)
        );

        // Initial validation for purchase limits
        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            purchaseLog.RoundId,
            purchaseLog.AvatarAddress
        );

        if (refreshTicketStatusPerRound is not null)
        {
            if (
                refreshTicketStatusPerRound.PurchaseCount + purchaseLog.PurchaseCount
                > season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound
            )
            {
                await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                    purchaseLog,
                    rtpl =>
                    {
                        rtpl.PurchaseStatus = PurchaseStatus.NO_REMAINING_PURCHASE_COUNT;
                    }
                );
                return "NO_REMAINING_PURCHASE_COUNT";
            }
        }
        else if (purchaseLog.PurchaseCount > season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound)
        {
            await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                purchaseLog,
                rtpl =>
                {
                    rtpl.PurchaseStatus = PurchaseStatus.NO_REMAINING_PURCHASE_COUNT;
                }
            );
            return "NO_REMAINING_PURCHASE_COUNT";
        }

        if (!await ValidateUsedTxId(purchaseLog.TxId, purchaseLogId))
        {
            await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                purchaseLog,
                rtpl =>
                {
                    rtpl.PurchaseStatus = PurchaseStatus.DUPLICATE_TRANSACTION;
                }
            );
            return $"{purchaseLog.TxId} is used tx";
        }

        await _ticketRepo.UpdateRefreshTicketPurchaseLog(
            purchaseLog,
            rtpl =>
            {
                rtpl.PurchaseStatus = PurchaseStatus.TRACKING;
            }
        );

        await _txTrackingService.TrackTransactionAsync(
            purchaseLog.TxId,
            async status =>
            {
                await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                    purchaseLog,
                    rtpl =>
                    {
                        rtpl.TxStatus = status.ToModelTxStatus();
                    }
                );
            },
            async successResponse =>
            {
                var taActionValue = await FetchAndSearchTransferAssetAction(purchaseLog.TxId);

                if (taActionValue is null)
                {
                    await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                        purchaseLog,
                        rtpl =>
                        {
                            rtpl.PurchaseStatus = PurchaseStatus.NOT_FOUND_TRANSFER_ASSETS_ACTION;
                        }
                    );
                    processResult = $"Not found tx for {purchaseLog.TxId}";
                }
                else
                {
                    if (taActionValue!.Recipient != _recipientAddress)
                    {
                        await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                            purchaseLog,
                            rtpl =>
                            {
                                rtpl.PurchaseStatus = PurchaseStatus.INVALID_RECIPIENT;
                            }
                        );
                        processResult = $"{_recipientAddress} is not ops address";
                    }
                    else
                    {
                        var requiredAmount = await CalcRequiredAmount(
                            season,
                            purchaseLog.RoundId,
                            purchaseLog.AvatarAddress,
                            purchaseLog.PurchaseCount
                        );

                        if (!ValidateCostPaid(taActionValue, requiredAmount))
                        {
                            await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                                purchaseLog,
                                rtpl =>
                                {
                                    rtpl.PurchaseStatus = PurchaseStatus.INSUFFICIENT_PAYMENT;
                                }
                            );

                            processResult =
                                $"Insufficient payment. paid: {taActionValue.Amount} required: {requiredAmount}";
                        }
                        else
                        {
                            processResult = await ProcessTicketPurchaseAsync(season, purchaseLog, requiredAmount);
                        }
                    }
                }
            },
            async failureResponse =>
            {
                await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                    purchaseLog,
                    rtpl =>
                    {
                        rtpl.TxStatus = Shared.Models.Enums.TxStatus.FAILURE;
                        rtpl.PurchaseStatus = PurchaseStatus.TX_FAILED;
                        rtpl.ExceptionNames = failureResponse.ExceptionNames is not null
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
            null
        );
        var refreshTicketPurchaseLog = await _ticketRepo.GetRefreshTicketPurchaseLogByTxId(
            txId,
            purchaseLogId
        );
        return battleTicketPurchaseLog is null && refreshTicketPurchaseLog is null;
    }

    private async Task<decimal> CalcRequiredAmount(
        Season season,
        int roundId,
        Address avatarAddress,
        int purchaseCount
    )
    {
        var requiredAmount = 0m;

        var refreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            roundId,
            avatarAddress
        );
        var purchasedCount = refreshTicketStatusPerRound is null
            ? 0
            : refreshTicketStatusPerRound.PurchaseCount;

        for (int i = 0; i < purchaseCount; i++)
        {
            requiredAmount += season.RefreshTicketPolicy.GetPrice(purchasedCount + i);
        }

        return requiredAmount;
    }

    private async Task<bool> UpdateTicket(Season season, RefreshTicketPurchaseLog purchaseLog)
    {
        var existRefreshTicketStatusPerRound = await _ticketRepo.GetRefreshTicketStatusPerRound(
            purchaseLog.RoundId,
            purchaseLog.AvatarAddress
        );

        if (existRefreshTicketStatusPerRound is null)
        {
            await _ticketRepo.AddRefreshTicketStatusPerRound(
                purchaseLog.SeasonId,
                purchaseLog.RoundId,
                purchaseLog.AvatarAddress,
                season.RefreshTicketPolicyId,
                season.RefreshTicketPolicy.DefaultTicketsPerRound + purchaseLog.PurchaseCount,
                0,
                purchaseLog.PurchaseCount
            );
        }
        else
        {
            var updated = await _ticketRepo.TryUpdateRefreshTicketStatusPerRound(
                purchaseLog.RoundId,
                purchaseLog.AvatarAddress,
                purchaseLog.PurchaseCount,
                season.RefreshTicketPolicy.MaxPurchasableTicketsPerRound
            );

            if (!updated)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string> ProcessTicketPurchaseAsync(
        Season season,
        RefreshTicketPurchaseLog purchaseLog,
        decimal requiredAmount
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var updated = await UpdateTicket(season, purchaseLog);
            if (!updated)
            {
                await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                    purchaseLog,
                    rtpl =>
                    {
                        rtpl.PurchaseStatus = PurchaseStatus.NO_REMAINING_PURCHASE_COUNT;
                    }
                );
                await transaction.RollbackAsync();
                return "MAX_TICKETS_REACHED";
            }

            await _ticketRepo.UpdateRefreshTicketPurchaseLog(
                purchaseLog,
                rtpl =>
                {
                    rtpl.PurchaseStatus = PurchaseStatus.SUCCESS;
                    rtpl.AmountPaid = requiredAmount;
                }
            );
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

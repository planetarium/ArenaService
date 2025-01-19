using System.Text.RegularExpressions;
using ArenaService.Client;
using ArenaService.Constants;
using ArenaService.Exceptions;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Options;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Utils;
using ArenaService.Views;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArenaService.Worker;

public class RefreshProcessor
{
    private readonly Address _recipientAddress;
    private readonly Codec Codec = new();
    private readonly ILogger<RefreshProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IParticipantRepository _participantRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IRefreshRequestRepository _refreshRequestRepo;
    private readonly ISpecifyOpponentsService _specifyOpponentsService;
    private readonly ITxTrackingService _txTrackingService;

    public RefreshProcessor(
        ILogger<RefreshProcessor> logger,
        IHeadlessClient client,
        IParticipantRepository participantRepo,
        IRefreshRequestRepository refreshRequestRepo,
        ISpecifyOpponentsService specifyOpponentsService,
        IAvailableOpponentRepository availableOpponentRepo,
        ITxTrackingService txTrackingService,
        IOptions<OpsConfigOptions> options
    )
    {
        _logger = logger;
        _client = client;
        _refreshRequestRepo = refreshRequestRepo;
        _participantRepo = participantRepo;
        _specifyOpponentsService = specifyOpponentsService;
        _availableOpponentRepo = availableOpponentRepo;
        _txTrackingService = txTrackingService;
        _recipientAddress = new Address(options.Value.RecipientAddress);
    }

    public async Task<string> ProcessAsync(int refreshRequestId, TxId txId)
    {
        _logger.LogInformation($"Calc ao: {refreshRequestId}, {txId}");

        var refreshRequest = await _refreshRequestRepo.GetRefreshRequestByIdAsync(
            refreshRequestId,
            query => query.Include(p => p.RefreshPriceDetail)
        );

        if (refreshRequest == null)
        {
            return $"Not found {refreshRequestId}";
        }

        string processResult = "before track";

        await _txTrackingService.TrackTransactionAsync(
            txId,
            async status =>
            {
                await _refreshRequestRepo.UpdateRefreshRequestAsync(
                    refreshRequest,
                    rr =>
                    {
                        rr.TxStatus = status.ToModelTxStatus();
                    }
                );
            },
            async successResponse =>
            {
                _logger.LogInformation($"Tx succeeded!");

                if (await ValidateCostPaid(txId, refreshRequest.RefreshPriceDetail.Price))
                {
                    if (await ValidateUsedTxId(txId))
                    {
                        await UpdateRefreshRequest(refreshRequest);
                        processResult = "success";
                    }

                    processResult = $"{txId} is used tx";
                }
                else
                {
                    await _refreshRequestRepo.UpdateRefreshRequestAsync(
                        refreshRequest,
                        rr =>
                        {
                            rr.RefreshStatus = RefreshStatus.COST_VALIDATION_FAILED;
                        }
                    );
                    processResult = "cost validation failed";
                }
            },
            txId =>
            {
                throw new TimeoutException($"Transaction timed out for ID: {txId}");
            }
        );

        return processResult;
    }

    private async Task<bool> ValidateCostPaid(TxId txId, double refreshPrice)
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
            var (actionType, actionValues) = DeconstructActionPlainValue(action);

            var actionTypeStr = actionType switch
            {
                Integer integer => integer.ToString(),
                Text text => (string)text,
                _ => null
            };

            if (actionTypeStr is null || actionValues is null)
            {
                continue;
            }

            if (Regex.IsMatch(actionTypeStr, "^transfer_asset[0-9]*$"))
            {
                var taActionValues = TransferAssetsParser.ParseActionPayload(
                    (Dictionary)actionValues
                );

                if (taActionValues.Recipient == _recipientAddress)
                {
                    if (taActionValues.Amount.Currency.Ticker == "NCG")
                    {
                        double calculatedAmount =
                            (double)taActionValues.Amount.MajorUnit
                            + (double)taActionValues.Amount.MinorUnit
                                / Math.Pow(10, taActionValues.Amount.Currency.DecimalPlaces);

                        if (Math.Abs(calculatedAmount - refreshPrice) < 0.0001)
                        {
                            return true;
                        }
                        else
                        {
                            _logger.LogInformation(
                                $"The refresh price does not match the transaction value. paid: {calculatedAmount} required: {refreshPrice}"
                            );
                            return false;
                        }
                    }

                    return true;
                }
                else
                {
                    _logger.LogInformation(
                        $"{taActionValues.Recipient} recipients is not ops account"
                    );
                    return false;
                }
            }
        }

        return false;
    }

    private async Task<bool> ValidateUsedTxId(TxId txId)
    {
        var refreshRequest = await _refreshRequestRepo.GetRefreshRequestByTxIdAsync(txId);
        return refreshRequest is null;
    }

    private static (IValue? typeId, IValue? values) DeconstructActionPlainValue(
        IValue actionPlainValue
    )
    {
        if (actionPlainValue is not Dictionary actionPlainValueDict)
        {
            return (null, null);
        }

        var actionType = actionPlainValueDict.ContainsKey("type_id")
            ? actionPlainValueDict["type_id"]
            : null;
        var actionPlainValueInternal = actionPlainValueDict.ContainsKey("values")
            ? actionPlainValueDict["values"]
            : null;
        return (actionType, actionPlainValueInternal);
    }

    private async Task UpdateRefreshRequest(RefreshRequest refreshRequest)
    {
        var opponents = await _specifyOpponentsService.SpecifyOpponentsAsync(
            new Address(refreshRequest.AvatarAddress),
            refreshRequest.SeasonId,
            refreshRequest.RoundId
        );
        await _refreshRequestRepo.UpdateRefreshRequestAsync(
            refreshRequest,
            rr =>
            {
                rr.SpecifiedOpponentAvatarAddresses = opponents
                    .Select(o => o.AvatarAddress.ToHex())
                    .ToList();
                rr.IsCostPaid = true;
                rr.RefreshStatus = RefreshStatus.SUCCESS;
            }
        );
        await _availableOpponentRepo.AddAvailableOpponents(
            refreshRequest.SeasonId,
            refreshRequest.RoundId,
            new Address(refreshRequest.AvatarAddress),
            refreshRequest.Id,
            opponents.Select(o => (o.AvatarAddress, o.GroupId)).ToList()
        );
        await _participantRepo.UpdateLastRefreshRequestId(
            refreshRequest.SeasonId,
            new Address(refreshRequest.AvatarAddress),
            refreshRequest.Id
        );
    }
}

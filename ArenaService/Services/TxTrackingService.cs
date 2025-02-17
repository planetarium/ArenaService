using ArenaService.Client;
using Libplanet.Types.Tx;

namespace ArenaService.Services;

public interface ITxTrackingService
{
    Task TrackTransactionAsync(
        TxId txId,
        Func<TxStatus, Task> onStatusUpdated,
        Func<IGetTransactionResult_Transaction_TransactionResult, Task> onFailureResponse,
        Func<IGetTransactionResult_Transaction_TransactionResult, Task> onSuccessResponse,
        Func<TxId, Task> onTimeout
    );
}

public class TxTrackingService : ITxTrackingService
{
    private readonly ILogger<TxTrackingService> _logger;
    private readonly IHeadlessClient _client;

    public TxTrackingService(ILogger<TxTrackingService> logger, IHeadlessClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task TrackTransactionAsync(
        TxId txId,
        Func<TxStatus, Task> onStatusUpdated,
        Func<IGetTransactionResult_Transaction_TransactionResult, Task> onFailureResponse,
        Func<IGetTransactionResult_Transaction_TransactionResult, Task> onSuccessResponse,
        Func<TxId, Task> onTimeout
    )
    {
        _logger.LogInformation($"Starting to track transaction: {txId}");

        for (int attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                var txResultResponse = await _client.GetTransactionResult.ExecuteAsync(
                    txId.ToString()
                );

                if (txResultResponse.Data is null)
                {
                    _logger.LogInformation("Transaction result is null. Retrying...");
                    await Task.Delay(2000);
                    continue;
                }

                var txStatus = txResultResponse.Data.Transaction.TransactionResult.TxStatus;

                await onStatusUpdated(txStatus);

                switch (txStatus)
                {
                    case TxStatus.Success:
                        _logger.LogInformation("Transaction succeeded.");
                        var successResponse = txResultResponse.Data.Transaction.TransactionResult;
                        await onSuccessResponse(successResponse);
                        return;

                    case TxStatus.Failure:
                        _logger.LogWarning("Transaction failed.");
                        var failureResponse = txResultResponse.Data.Transaction.TransactionResult;
                        await onFailureResponse(failureResponse);
                        return;

                    default:
                        _logger.LogInformation(
                            $"Transaction is in progress. Current status: {txStatus}"
                        );
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while tracking transaction {txId}: {ex}");
            }

            await Task.Delay(2000);
        }

        _logger.LogWarning($"Transaction tracking timed out for {txId}.");
        await onTimeout(txId);
    }
}

using ArenaService.Client;

namespace ArenaService.Worker;

public class BattleTaskProcessor
{
    private readonly ILogger<BattleTaskProcessor> _logger;
    private readonly IHeadlessClient _client;

    public BattleTaskProcessor(ILogger<BattleTaskProcessor> logger, IHeadlessClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task ProcessAsync(string txId, int battleLogId)
    {
        var response = await _client.GetTransactionResult.ExecuteAsync(txId);

        _logger.LogInformation(
            $"Temp: {txId}, {response.Data?.Transaction.TransactionResult.TxStatus}"
        );

        await Task.Delay(300000);
    }
}

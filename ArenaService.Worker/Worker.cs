using ArenaService.Worker.Rpc;

namespace ArenaService.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RpcClient _rpcClient;

    public Worker(ILogger<Worker> logger, RpcClient client)
    {
        _logger = logger;
        _rpcClient = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            _logger.LogInformation("worker");
            await Task.Delay(1000, stoppingToken);
        }
    }
}

namespace ArenaService;

public class RpcService(RpcClient rpcClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = rpcClient.StartAsync(cancellationToken);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await rpcClient.StopAsync(cancellationToken);
    }
}

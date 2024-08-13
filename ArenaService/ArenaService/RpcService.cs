namespace ArenaService;

public class RpcService(RpcClient rpcClient) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await rpcClient.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await rpcClient.StopAsync(cancellationToken);
    }
}

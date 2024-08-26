namespace ArenaService;

public class RpcService(RpcClient rpcClient, RpcNodeHealthCheck rpcNodeHealthCheck) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = rpcClient.StartAsync(cancellationToken);
        _ = HealthCheck(cancellationToken);
        await Task.CompletedTask;
    }

    private async Task HealthCheck(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var retry = 0;
            while (!rpcClient.Ready && retry < 3)
            {
                await Task.Delay((3 - retry) * 1000, cancellationToken);
                retry++;
            }
            rpcNodeHealthCheck.ConnectCompleted = rpcClient.Ready;
            await Task.Delay(3000, cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await rpcClient.StopAsync(cancellationToken);
    }
}

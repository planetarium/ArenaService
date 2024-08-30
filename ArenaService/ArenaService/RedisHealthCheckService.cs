namespace ArenaService;

public class RedisHealthCheckService(IRedisArenaParticipantsService redisArenaParticipantsService, RedisHealthCheck redisHealthCheck) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = HealthCheck(cancellationToken);
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }


    private async Task HealthCheck(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            var seasonKey = await redisArenaParticipantsService.GetSeasonKeyAsync();
            redisHealthCheck.ConnectCompleted = !string.IsNullOrEmpty(seasonKey);
            await Task.Delay(8000, cancellationToken);
        }
    }
}

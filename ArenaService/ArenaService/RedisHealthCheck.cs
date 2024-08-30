using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArenaService;

public class RedisHealthCheck : IHealthCheck
{
    private volatile bool _ready;

    public bool ConnectCompleted
    {
        get => _ready;
        set => _ready = value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (ConnectCompleted)
        {
            return Task.FromResult(HealthCheckResult.Healthy("redis caching completed"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("redis caching not completed"));
    }
}

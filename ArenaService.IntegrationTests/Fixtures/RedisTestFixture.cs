using StackExchange.Redis;

namespace ArenaService.IntegrationTests.Fixtures;

public class RedisTestFixture : IDisposable
{
    public IConnectionMultiplexer Redis { get; }

    public RedisTestFixture()
    {
        var redisConfiguration = new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" },
            AllowAdmin = true,
        };
        Redis = ConnectionMultiplexer.Connect(redisConfiguration);

        Redis.GetDatabase().Execute("FLUSHALL");
    }

    public IDatabase GetDatabase(int databaseNumber = 0)
    {
        return Redis.GetDatabase(databaseNumber);
    }

    public void Dispose()
    {
        Redis?.Dispose();
    }
}

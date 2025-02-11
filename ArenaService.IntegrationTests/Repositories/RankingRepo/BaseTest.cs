using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using StackExchange.Redis;

namespace ArenaService.IntegrationTests.Repositories.RankingRepo;

public abstract class BaseTest : IClassFixture<RedisTestFixture>
{
    protected RankingRepository Repository { get; }
    protected IDatabase Database { get; }

    protected BaseTest(RedisTestFixture fixture, int databaseNumber = 0)
    {
        Database = fixture.GetDatabase(databaseNumber);
        Repository = new RankingRepository(fixture.Redis, databaseNumber);
    }
}

using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using StackExchange.Redis;

namespace ArenaService.IntegrationTests.Repositories;

public abstract class BaseTest : IClassFixture<RedisTestFixture>
{
    protected RankingRepository RankingRepository { get; }
    protected ClanRankingRepository ClanRankingRepository { get; }
    protected AllClanRankingRepository AllClanRankingRepository { get; }
    protected IDatabase Database { get; }

    protected BaseTest(RedisTestFixture fixture, int databaseNumber = 0)
    {
        Database = fixture.GetDatabase(databaseNumber);
        RankingRepository = new RankingRepository(fixture.Redis, databaseNumber);
        ClanRankingRepository = new ClanRankingRepository(fixture.Redis, databaseNumber);
        AllClanRankingRepository = new AllClanRankingRepository(fixture.Redis, databaseNumber);
    }
}

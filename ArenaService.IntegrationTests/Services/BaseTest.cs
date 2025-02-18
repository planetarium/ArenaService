using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Repositories;
using StackExchange.Redis;

namespace ArenaService.IntegrationTests.Services;

public abstract class BaseTest : IClassFixture<RedisTestFixture>
{
    protected IRankingService RankingService { get; }
    protected AllClanRankingRepository AllClanRankingRepository { get; }
    protected ClanRankingRepository ClanRankingRepository { get; }
    protected RankingRepository RankingRepository { get; }
    protected IDatabase Database { get; }

    protected BaseTest(RedisTestFixture fixture, int databaseNumber = 0)
    {
        Database = fixture.GetDatabase(databaseNumber);

        AllClanRankingRepository = new AllClanRankingRepository(fixture.Redis, databaseNumber);
        ClanRankingRepository = new ClanRankingRepository(fixture.Redis, databaseNumber);
        RankingRepository = new RankingRepository(fixture.Redis, databaseNumber);
        RankingService = new RankingService(
            RankingRepository,
            ClanRankingRepository,
            AllClanRankingRepository
        );
    }
}

using ArenaService.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class MoveOneFromGroupWithOthersTests : BaseTest
{
    public MoveOneFromGroupWithOthersTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 3) { }

    [Fact]
    public async Task MoveOneFromGroupWithOthersCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;
        
        string statusKey = string.Format(GroupRankingRepository.StatusKeyFormat, seasonId, roundId);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var address1 = new Address(TestUtils.GetRandomBytes(Address.Size));
        var address2 = new Address(TestUtils.GetRandomBytes(Address.Size));
        var address3 = new Address(TestUtils.GetRandomBytes(Address.Size));
        var initialScore = 1000;
        var newScore = 1001;

        string groupKey1000 = string.Format(
            GroupRankingRepository.GroupKeyFormat,
            seasonId,
            roundId,
            initialScore
        );
        string groupKey1001 = string.Format(
            GroupRankingRepository.GroupKeyFormat,
            seasonId,
            roundId,
            newScore
        );

        await Repository.UpdateScoreAsync(address1, seasonId, roundId, 0, initialScore, 100);
        await Repository.UpdateScoreAsync(address2, seasonId, roundId, 0, initialScore, 100);
        await Repository.UpdateScoreAsync(address3, seasonId, roundId, 0, initialScore, 100);

        await Repository.UpdateScoreAsync(address3, seasonId, roundId, initialScore, newScore, 100);

        var groupData = await Database.HashGetAsync(
            groupKey1001,
            string.Format(GroupRankingRepository.ParticipantKeyFormat, address3.ToHex())
        );
        Assert.Equal(newScore.ToString(), groupData);

        var oldGroupExists = await Database.HashExistsAsync(
            groupKey1000,
            string.Format(GroupRankingRepository.ParticipantKeyFormat, address3.ToHex())
        );
        Assert.False(oldGroupExists);
    }
}

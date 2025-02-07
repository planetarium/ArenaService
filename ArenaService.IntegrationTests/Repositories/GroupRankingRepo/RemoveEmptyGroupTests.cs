using ArenaService.Shared.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class RemoveEmptyGroupTests : BaseTest
{
    public RemoveEmptyGroupTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 4) { }

    [Fact]
    public async Task RemoveEmptyGroupCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;
        
        string statusKey = string.Format(GroupRankingRepository.StatusKeyFormat, seasonId, roundId);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var address = new Address(TestUtils.GetRandomBytes(Address.Size));
        var initialScore = 1000;
        var newScore = 1001;

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );
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

        await Repository.UpdateScoreAsync(address, seasonId, roundId, 0, initialScore, 100);

        await Repository.UpdateScoreAsync(address, seasonId, roundId, initialScore, newScore, 100);

        var groupData = await Database.HashGetAsync(
            groupKey1001,
            string.Format(GroupRankingRepository.ParticipantKeyFormat, address.ToHex())
        );
        Assert.Equal(newScore.ToString(), groupData);

        var oldGroupExists = await Database.HashExistsAsync(
            groupKey1000,
            string.Format(GroupRankingRepository.ParticipantKeyFormat, address.ToHex())
        );
        Assert.False(oldGroupExists);

        double? groupRankingScore = await Database.SortedSetScoreAsync(
            groupRankingKey,
            groupKey1000
        );
        Assert.Null(groupRankingScore);
    }
}

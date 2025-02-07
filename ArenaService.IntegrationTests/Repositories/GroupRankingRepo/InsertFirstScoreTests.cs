using ArenaService.Shared.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class InsertFirstScoreTests : BaseTest
{
    public InsertFirstScoreTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 2) { }

    [Fact]
    public async Task InsertFirstScoreCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;

        string statusKey = string.Format(GroupRankingRepository.StatusKeyFormat, seasonId, roundId);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var address = new Address(TestUtils.GetRandomBytes(Address.Size));
        var score = 1000;

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );
        string groupKey = string.Format(
            GroupRankingRepository.GroupKeyFormat,
            seasonId,
            roundId,
            score
        );
        string participantKey = string.Format(
            GroupRankingRepository.ParticipantKeyFormat,
            address.ToHex()
        );
        string memberKey = string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, score);

        await Repository.UpdateScoreAsync(address, seasonId, roundId, 0, score, 100);

        var groupData = await Database.HashGetAsync(groupKey, participantKey);
        Assert.Equal(score.ToString(), groupData);

        double? groupRankingScore = await Database.SortedSetScoreAsync(groupRankingKey, memberKey);
        Assert.Equal(score, groupRankingScore);
    }
}

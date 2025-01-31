using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class AllCaseTests : BaseTest
{
    public AllCaseTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 1) { }

    [Fact]
    public async Task AllCaseCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );

        var scores = new List<(Address Address, int InitialScore, int NewScore)>
        {
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1001),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1000),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1000),
            
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 2000, 3000),
            
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1500, 1600),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1600, 1600),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1600, 1500),
        };

        foreach (var (address, initialScore, _) in scores)
        {
            await Repository.UpdateScoreAsync(address, seasonId, roundId, 0, initialScore);
        }

        foreach (var (address, initialScore, newScore) in scores)
        {
            await Repository.UpdateScoreAsync(address, seasonId, roundId, initialScore, newScore);
        }

        foreach (var (address, _, newScore) in scores)
        {
            string participantKey = string.Format(
                GroupRankingRepository.ParticipantKeyFormat,
                address.ToHex()
            );
            string newGroupKey = string.Format(
                GroupRankingRepository.GroupKeyFormat,
                seasonId,
                roundId,
                newScore
            );

            var groupData = await Database.HashGetAsync(newGroupKey, participantKey);
            Assert.Equal(newScore.ToString(), groupData);

            double? newGroupRankingScore = await Database.SortedSetScoreAsync(
                groupRankingKey,
                newGroupKey
            );
            Assert.Equal(newScore, newGroupRankingScore);
        }
    }
}

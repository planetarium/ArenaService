using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;

namespace ArenaService.IntegrationTests.Services.RankingServiceTests;

public class UpdateAllClanRankingCorrectly : BaseTest
{
    public UpdateAllClanRankingCorrectly(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 5) { }

    [Fact]
    public async Task UpdateAllClanRankingTests()
    {
        var seasonId = 1;
        var roundId = 1;
        var roundInterval = 10;

        var clanIds = new List<int> { 1, 2, 3 };

        foreach (var clanId in clanIds)
        {
            var totalParticipants = 15;
            var rankingData = new List<(Libplanet.Crypto.Address AvatarAddress, int Score)>();

            for (int i = 0; i < totalParticipants; i++)
            {
                var address = new Libplanet.Crypto.Address(
                    TestUtils.GetRandomBytes(Libplanet.Crypto.Address.Size)
                );
                rankingData.Add((address, 1000 * clanId + i));
            }

            await ClanRankingRepository.InitRankingAsync(
                rankingData,
                clanId,
                seasonId,
                roundId,
                roundInterval
            );
        }

        await RankingService.UpdateAllClanRankingAsync(seasonId, roundId, roundInterval);

        string allClanRankingKey = string.Format(
            AllClanRankingRepository.ClanRankingKeyFormat,
            seasonId,
            roundId
        );

        foreach (var clanId in clanIds)
        {
            int expectedTotalScore = Enumerable.Range(5, 10).Sum(i => (1000 * clanId) + i);

            string clanKey = string.Format(AllClanRankingRepository.ClanKeyFormat, clanId);
            var resultScore = await Database.SortedSetScoreAsync(allClanRankingKey, clanKey);

            Assert.Equal(expectedTotalScore, resultScore);
        }
    }
}

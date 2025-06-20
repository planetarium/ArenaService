using ArenaService.Shared.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.ClanRankingRepo;

public class InitRankingCorrectly : BaseTest
{
    public InitRankingCorrectly(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 3) { }

    [Fact]
    public async Task InitRankingTests()
    {
        var seasonId = 1;
        var roundIndex = 1;
        var clanId = 1;
        var roundInterval = 10;

        string statusKey = string.Format(
            ClanRankingRepository.StatusKeyFormat,
            seasonId,
            roundIndex,
            clanId
        );

        var totalParticipants = 10;
        var rankingData = new List<(Address AvatarAddress, int Score)>();
        string rankingKey = string.Format(
            ClanRankingRepository.ClanRankingFormat,
            seasonId,
            roundIndex,
            clanId
        );

        for (int i = 0; i < totalParticipants; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            rankingData.Add((address, 1100 + i));
        }

        await ClanRankingRepository.InitRankingAsync(
            rankingData,
            clanId,
            seasonId,
            roundIndex,
            roundInterval
        );

        foreach (var (address, score) in rankingData)
        {
            string participantKey = string.Format(
                ClanRankingRepository.ParticipantKeyFormat,
                address.ToHex().ToLower()
            );

            var resultScore = await Database.SortedSetScoreAsync(rankingKey, participantKey);
            Assert.Equal(score, resultScore);
        }
        string clansKey = string.Format(ClanRankingRepository.ClansKeyFormat, seasonId, roundIndex);

        Assert.True(await Database.SetContainsAsync(clansKey, clanId));

        var storedStatus = await Database.StringGetAsync(statusKey);
        Assert.Equal(RankingStatus.DONE.ToString(), storedStatus);
    }
}

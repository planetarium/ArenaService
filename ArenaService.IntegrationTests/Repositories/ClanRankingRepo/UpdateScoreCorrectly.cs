using ArenaService.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.ClanRankingRepo;

public class UpdateScoreCorrectly : BaseTest
{
    public UpdateScoreCorrectly(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 2) { }

    [Fact]
    public async Task UpdateScoreTests()
    {
        var seasonId = 1;
        var roundId = 1;
        var clanId = 1;

        string statusKey = string.Format(
            ClanRankingRepository.StatusKeyFormat,
            seasonId,
            roundId,
            clanId
        );
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var totalParticipants = 10;
        var participants = new Dictionary<Address, int>();
        string rankingKey = string.Format(
            ClanRankingRepository.ClanRankingFormat,
            seasonId,
            roundId,
            clanId
        );

        for (int i = 0; i < totalParticipants; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            participants[address] = 1100 + i;
        }

        foreach (var (address, score) in participants)
        {
            string participantKey = string.Format(
                ClanRankingRepository.ParticipantKeyFormat,
                address.ToHex()
            );
            await ClanRankingRepository.UpdateScoreAsync(clanId, address, seasonId, roundId, score);

            var resultScore = await Database.SortedSetScoreAsync(rankingKey, participantKey);
            Assert.Equal(score, resultScore);
        }
    }
}

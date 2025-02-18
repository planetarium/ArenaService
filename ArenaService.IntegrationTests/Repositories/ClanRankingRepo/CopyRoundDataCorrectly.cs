using ArenaService.Shared.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.ClanRankingRepo;

public class CopyRoundDataCorrectly : BaseTest
{
    public CopyRoundDataCorrectly(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 4) { }

    [Fact]
    public async Task CopyRoundDataTests()
    {
        var seasonId = 1;
        var sourceRoundId = 1;
        var targetRoundId = 2;
        var clanId = 1;
        var roundInterval = 10;

        string sourceRankingKey = string.Format(
            ClanRankingRepository.ClanRankingFormat,
            seasonId,
            sourceRoundId,
            clanId
        );
        string targetRankingKey = string.Format(
            ClanRankingRepository.ClanRankingFormat,
            seasonId,
            targetRoundId,
            clanId
        );
        string sourceStatusKey = string.Format(
            ClanRankingRepository.StatusKeyFormat,
            seasonId,
            sourceRoundId,
            clanId
        );
        string targetStatusKey = string.Format(
            ClanRankingRepository.StatusKeyFormat,
            seasonId,
            targetRoundId,
            clanId
        );
        string sourceClansKey = string.Format(
            ClanRankingRepository.ClansKeyFormat,
            seasonId,
            sourceRoundId
        );
        string targetClansKey = string.Format(
            ClanRankingRepository.ClansKeyFormat,
            seasonId,
            sourceRoundId
        );

        await Database.StringSetAsync(sourceStatusKey, RankingStatus.DONE.ToString());

        var totalParticipants = 10;
        var rankingData = new List<(Address AvatarAddress, int Score)>();

        for (int i = 0; i < totalParticipants; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            rankingData.Add((address, 1100 + i));
        }

        foreach (var (address, score) in rankingData)
        {
            string participantKey = string.Format(
                ClanRankingRepository.ParticipantKeyFormat,
                address.ToHex().ToLower()
            );
            await Database.SortedSetAddAsync(sourceRankingKey, participantKey, score);
        }

        await Database.SetAddAsync(sourceClansKey, clanId.ToString());

        await ClanRankingRepository.CopyRoundDataAsync(
            clanId,
            seasonId,
            sourceRoundId,
            targetRoundId,
            roundInterval
        );

        foreach (var (address, score) in rankingData)
        {
            string participantKey = string.Format(
                ClanRankingRepository.ParticipantKeyFormat,
                address.ToHex().ToLower()
            );

            var resultScore = await Database.SortedSetScoreAsync(targetRankingKey, participantKey);
            Assert.Equal(score, resultScore);
        }

        Assert.True(await Database.SetContainsAsync(targetClansKey, clanId));

        var storedStatus = await Database.StringGetAsync(targetStatusKey);
        Assert.Equal(RankingStatus.DONE.ToString(), storedStatus);
    }
}

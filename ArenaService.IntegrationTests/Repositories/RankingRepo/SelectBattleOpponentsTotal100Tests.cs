using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Constants;
using ArenaService.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.RankingRepo;

public class SelectBattleOpponentsTotal100Tests : BaseTest
{
    public SelectBattleOpponentsTotal100Tests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 0) { }

    [Fact]
    public async Task SelectBattleOpponentsTotal100Correctly()
    {
        var seasonId = 1;
        var roundId = 1;

        string statusKey = string.Format(RankingRepository.StatusKeyFormat, seasonId, roundId);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var totalParticipants = 100;
        var participants = new Dictionary<int, Address>();
        string rankingKey = string.Format(RankingRepository.RankingKeyFormat, seasonId, roundId);

        for (int i = 0; i < totalParticipants; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            var score = i;
            participants[score] = address;
        }

        foreach (var (score, address) in participants)
        {
            string participantKey = string.Format(
                RankingRepository.ParticipantKeyFormat,
                address.ToHex()
            );

            await Database.SortedSetAddAsync(rankingKey, participantKey, score);
        }

        // 랜덤에 기반하기 때문에 100번 반복해서 검증합니다.
        for (int i = 0; i < 100; i++)
        {
            foreach (var (score, address) in participants)
            {
                await CheckRankInRange(seasonId, roundId, address, rankingKey);
            }
        }
    }

    private async Task CheckRankInRange(
        int seasonId,
        int roundId,
        Address avatarAddress,
        string rankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            avatarAddress,
            seasonId,
            roundId
        );
        Assert.Equal(5, opponents.Count);
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max, _, _) = OpponentGroupConstants.Groups[groupId];
            string participantKey = string.Format(
                RankingRepository.ParticipantKeyFormat,
                opponent.AvatarAddress.ToHex()
            );

            long? opponentRank = await Database.SortedSetRankAsync(
                rankingKey,
                participantKey,
                Order.Descending
            );
            long totalRankings = await Database.SortedSetLengthAsync(rankingKey);

            Assert.NotEqual(avatarAddress, opponent.AvatarAddress);
            Assert.InRange(opponentRank.Value + 1, totalRankings * min, totalRankings * max);
        }
    }
}

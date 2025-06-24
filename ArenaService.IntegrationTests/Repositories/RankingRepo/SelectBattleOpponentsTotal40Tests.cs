using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.RankingRepo;

public class SelectBattleOpponentsTotal40Tests : BaseTest
{
    public SelectBattleOpponentsTotal40Tests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 0) { }

    [Fact]
    public async Task SelectBattleOpponentsTotal40Correctly()
    {
        var seasonId = 1;
        var roundIndex = 1;

        string statusKey = string.Format(RankingRepository.StatusKeyFormat, seasonId, roundIndex);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        var totalParticipants = 41;
        var participants = new Dictionary<int, Address>();
        string rankingKey = string.Format(RankingRepository.RankingKeyFormat, seasonId, roundIndex);

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
                address.ToHex().ToLower()
            );

            await Database.SortedSetAddAsync(rankingKey, participantKey, score);
        }

        // 랜덤에 기반하기 때문에 100번 반복해서 검증합니다.
        for (int i = 0; i < 100; i++)
        {
            foreach (var (score, address) in participants)
            {
                await CheckRankInRange(seasonId, roundIndex, address, rankingKey);
            }
        }
    }

    private async Task CheckRankInRange(
        int seasonId,
        int roundIndex,
        Address avatarAddress,
        string rankingKey
    )
    {
        var opponents = await RankingRepository.SelectBattleOpponentsAsync(
            avatarAddress,
            seasonId,
            roundIndex,
            false
        );
        Assert.Equal(5, opponents.Count);
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max, _, _) = OpponentGroupConstants.Groups[groupId];
            string participantKey = string.Format(
                RankingRepository.ParticipantKeyFormat,
                opponent.AvatarAddress.ToHex().ToLower()
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

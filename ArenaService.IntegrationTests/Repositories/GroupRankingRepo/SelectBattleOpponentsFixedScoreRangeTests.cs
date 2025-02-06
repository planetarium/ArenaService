using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArenaService.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;
using StackExchange.Redis;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class SelectBattleOpponentsFixedScoreRangeTests : BaseTest
{
    public SelectBattleOpponentsFixedScoreRangeTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 8) { }

    [Fact]
    public async Task SelectBattleOpponentsFixedScoreRangeCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;

        string statusKey = string.Format(GroupRankingRepository.StatusKeyFormat, seasonId, roundId);
        await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );

        var totalParticipants = 100;
        var participants = new Dictionary<int, Address>();

        for (int i = 0; i < totalParticipants; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            var score = i;
            participants[score] = address;
        }

        foreach (var (score, address) in participants)
        {
            string groupKey = string.Format(
                GroupRankingRepository.GroupKeyFormat,
                seasonId,
                roundId,
                score
            );
            string memberKey = string.Format(
                GroupRankingRepository.GroupRankingMemberKeyFormat,
                score
            );

            await Database.HashSetAsync(
                groupKey,
                string.Format(GroupRankingRepository.ParticipantKeyFormat, address.ToHex()),
                score
            );
            await Database.SortedSetAddAsync(groupRankingKey, memberKey, score);
        }

        // 랜덤에 기반하기 때문에 100번 반복해서 검증합니다.
        for (int i = 0; i < 100; i++)
        {
            await TestRank100(seasonId, roundId, participants[0], 0, groupRankingKey);
            await TestRank70(seasonId, roundId, participants[30], 30, groupRankingKey);
            await TestRank50(seasonId, roundId, participants[50], 50, groupRankingKey);
            await TestRank30(seasonId, roundId, participants[70], 70, groupRankingKey);
            await TestRank1(seasonId, roundId, participants[99], 99, groupRankingKey);
        }
    }

    private async Task TestRank100(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score,
        string groupRankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            seasonId,
            roundId,
            avatarAddress,
            score
        );
        var checkGroupRange = new Dictionary<int, (long Min, long Max)>
        {
            { 1, (20, 40) },
            { 2, (40, 80) },
            { 3, (80, 100) },
            { 4, (80, 100) }, // 100등이 100등이므로 3번 그룹에서 가져왔을 것
            { 5, (80, 100) }, // 100등이 100등이므로 3번 그룹에서 가져왔을 것
        };
        Assert.Equal(5, opponents.Count);
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max) = checkGroupRange[groupId];

            long? opponentRank = await Database.SortedSetRankAsync(
                groupRankingKey,
                string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, opponent.Score),
                Order.Descending
            );

            Assert.InRange(opponentRank.Value + 1, min, max);
        }
    }

    private async Task TestRank70(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score,
        string groupRankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            seasonId,
            roundId,
            avatarAddress,
            score
        );
        var checkGroupRange = new Dictionary<int, (long Min, long Max)>
        {
            { 1, (14, 28) },
            { 2, (28, 56) },
            { 3, (56, 86) },
            { 4, (84, 100) },
            { 5, (84, 100) },
        };
        Assert.Equal(5, opponents.Count);
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max) = checkGroupRange[groupId];

            long? opponentRank = await Database.SortedSetRankAsync(
                groupRankingKey,
                string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, opponent.Score),
                Order.Descending
            );

            Assert.InRange(opponentRank.Value + 1, min, max);
        }
    }

    private async Task TestRank50(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score,
        string groupRankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            seasonId,
            roundId,
            avatarAddress,
            score
        );
        var checkGroupRange = new Dictionary<int, (long Min, long Max)>
        {
            { 1, (10, 20) },
            { 2, (20, 40) },
            { 3, (40, 60) },
            { 4, (60, 90) },
            { 5, (90, 100) },
        };
        Assert.Equal(5, opponents.Count);
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max) = checkGroupRange[groupId];

            long? opponentRank = await Database.SortedSetRankAsync(
                groupRankingKey,
                string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, opponent.Score),
                Order.Descending
            );

            Assert.InRange(opponentRank.Value + 1, min, max);
        }
    }

    private async Task TestRank30(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score,
        string groupRankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            seasonId,
            roundId,
            avatarAddress,
            score
        );
        Assert.Equal(5, opponents.Count);
        var checkGroupRange = new Dictionary<int, (long Min, long Max)>
        {
            { 1, (6, 12) },
            { 2, (12, 24) },
            { 3, (24, 37) },
            { 4, (36, 55) },
            { 5, (54, 90) },
        };
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max) = checkGroupRange[groupId];

            long? opponentRank = await Database.SortedSetRankAsync(
                groupRankingKey,
                string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, opponent.Score),
                Order.Descending
            );

            Assert.InRange(opponentRank.Value + 1, min, max);
        }
    }

    private async Task TestRank1(
        int seasonId,
        int roundId,
        Address avatarAddress,
        int score,
        string groupRankingKey
    )
    {
        var opponents = await Repository.SelectBattleOpponentsAsync(
            seasonId,
            roundId,
            avatarAddress,
            score
        );
        Assert.Equal(5, opponents.Count);

        var checkGroupRange = new Dictionary<int, (long Min, long Max)>
        {
            { 1, (1, 2) },
            { 2, (1, 7) },
            { 3, (1, 7) },
            { 4, (1, 7) },
            { 5, (1, 7) },
        };
        foreach (var (groupId, opponent) in opponents)
        {
            var (min, max) = checkGroupRange[groupId];

            long? opponentRank = await Database.SortedSetRankAsync(
                groupRankingKey,
                string.Format(GroupRankingRepository.GroupRankingMemberKeyFormat, opponent.Score),
                Order.Descending
            );

            Assert.InRange(opponentRank.Value + 1, min, max);
        }
    }
}

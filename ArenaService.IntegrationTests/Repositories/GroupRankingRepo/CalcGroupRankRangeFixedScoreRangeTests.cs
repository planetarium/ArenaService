using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArenaService.Constants;
using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Xunit;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class CalcGroupRankRangeFixedScoreRangeTests : BaseTest
{
    public CalcGroupRankRangeFixedScoreRangeTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 7) { }

    [Fact]
    public async Task CalcGroupRankRange_ShouldReturnExpectedRanges()
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

        for (int i = 1; i <= totalParticipants; i++)
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

        foreach (var (score, address) in participants)
        {
            var groupRange = await Repository.CalcGroupRankRange(seasonId, roundId, score);

            await Verify(groupRange).UseParameters(address.ToHex());
            ;
        }
    }
}

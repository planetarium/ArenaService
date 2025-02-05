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

public class CalcGroupRankRangeFixed100ParticipantsTests : BaseTest
{
    public CalcGroupRankRangeFixed100ParticipantsTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 7) { }

    [Fact]
    public async Task CalcGroupRankRangeShouldReturnExpectedRanges()
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

        // Test 100 rank
        var groupRange100 = await Repository.CalcGroupRankRange(seasonId, roundId, 100);
        await Verify(groupRange100)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_100");

        // Test 90 rank
        var groupRange90 = await Repository.CalcGroupRankRange(seasonId, roundId, 90);
        await Verify(groupRange90)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_90");

        // Test 70 rank
        var groupRange70 = await Repository.CalcGroupRankRange(seasonId, roundId, 70);
        await Verify(groupRange70)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_70");

        // Test 50 rank
        var groupRange50 = await Repository.CalcGroupRankRange(seasonId, roundId, 50);
        await Verify(groupRange50)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_50");

        // Test 30 rank
        var groupRange30 = await Repository.CalcGroupRankRange(seasonId, roundId, 30);
        await Verify(groupRange30)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_30");

        // Test 10 rank
        var groupRange10 = await Repository.CalcGroupRankRange(seasonId, roundId, 10);
        await Verify(groupRange10)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_10");

        // Test 1 rank
        var groupRange1 = await Repository.CalcGroupRankRange(seasonId, roundId, 1);
        await Verify(groupRange1)
            .UseFileName($"{nameof(CalcGroupRankRangeFixed100ParticipantsTests)}_1");
    }
}

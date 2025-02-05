// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using ArenaService.Constants;
// using ArenaService.IntegrationTests.Fixtures;
// using ArenaService.Repositories;
// using Libplanet.Crypto;
// using Xunit;

// namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

// public class SelectBattleOpponentsFixedScoreRangeTests : BaseTest
// {
//     public SelectBattleOpponentsFixedScoreRangeTests(RedisTestFixture fixture)
//         : base(fixture, databaseNumber: 8) { }

//     [Fact]
//     public async Task SelectBattleOpponentsFixedScoreRangeCorrectly()
//     {
//         var seasonId = 1;
//         var roundId = 1;

//         string statusKey = string.Format(GroupRankingRepository.StatusKeyFormat, seasonId, roundId);
//         await Database.StringSetAsync(statusKey, RankingStatus.DONE.ToString());

//         string groupRankingKey = string.Format(
//             GroupRankingRepository.GroupedRankingKeyFormat,
//             seasonId,
//             roundId
//         );

//         var totalParticipants = 100;
//         var participants = new Dictionary<int, Address>();

//         for (int i = 1; i <= totalParticipants; i++)
//         {
//             var address = new Address(TestUtils.GetRandomBytes(Address.Size));
//             var score = i;
//             participants[score] = address;
//         }

//         foreach (var (score, address) in participants)
//         {
//             string groupKey = string.Format(
//                 GroupRankingRepository.GroupKeyFormat,
//                 seasonId,
//                 roundId,
//                 score
//             );
//             string memberKey = string.Format(
//                 GroupRankingRepository.GroupRankingMemberKeyFormat,
//                 score
//             );

//             await Database.HashSetAsync(
//                 groupKey,
//                 string.Format(GroupRankingRepository.ParticipantKeyFormat, address.ToHex()),
//                 score
//             );
//             await Database.SortedSetAddAsync(groupRankingKey, memberKey, score);
//         }

//         foreach (var (score, address) in participants)
//         {
//             var opponents = await Repository.SelectBattleOpponentsAsync(
//                 address,
//                 score,
//                 seasonId,
//                 roundId
//             );

//             for (int groupId = 1; groupId <= 5; groupId++)
//             {
//                 var (startRank, endRank) = CalculateGroupRange(
//                     groupId,
//                     totalParticipants,
//                     totalParticipants - score + 1
//                 );

//                 if (expectedGroups[groupId].Count == 0)
//                 {
//                     Assert.True(
//                         opponents[groupId] == null,
//                         $"Score {testScore}의 그룹 {groupId}는 상대가 없어야 합니다."
//                     );
//                 }
//                 else
//                 {
//                     Assert.NotNull(opponents[groupId]);
//                     var opponentScore = opponents[groupId]!.Value.Score;
//                     var expectedList = expectedGroups[groupId];
//                     Assert.True(
//                         expectedList.Contains(opponentScore),
//                         $"Score {testScore}의 그룹 {groupId}에서 예상치 못한 상대가 선택됨: {opponentScore}"
//                     );
//                 }
//             }
//         }
//     }

//     private (int, int) CalculateGroupRange(int groupId, int totalRanking, int myRanking)
//     {
//         var rankRange = OpponentGroupConstants.Groups[groupId];

//         int startRank = (int)Math.Ceiling(myRanking * rankRange.MinRange);
//         int endRank = (int)Math.Ceiling(myRanking * rankRange.MaxRange);

//         startRank = Math.Max(1, startRank);
//         startRank = Math.Min(totalRanking, startRank);
//         endRank = Math.Min(totalRanking, endRank);

//         return (startRank, endRank);
//     }
// }

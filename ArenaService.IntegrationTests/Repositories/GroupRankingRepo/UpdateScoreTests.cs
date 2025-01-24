using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class UpdateScoreTests : BaseTest
{
    private const int TotalScores = 10000;
    private static readonly Random Random = new();

    public UpdateScoreTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 5) { }

    [Fact]
    public async Task UpdateScoreCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );

        var scores = new List<(Address Address, int InitialScore, int NewScore)>();

        for (int i = 0; i < TotalScores; i++)
        {
            var address = new Address(TestUtils.GetRandomBytes(Address.Size));
            int initialScore = Random.Next(1000, 5000);
            int newScore = Random.Next(1000, 5000);
            scores.Add((address, initialScore, newScore));
        }

        foreach (var (address, initialScore, _) in scores)
        {
            // 초기 점수 설정
            await Repository.UpdateScoreAsync(address, seasonId, roundId, 0, initialScore);
        }

        foreach (var (address, initialScore, newScore) in scores)
        {
            // 점수 업데이트
            await Repository.UpdateScoreAsync(address, seasonId, roundId, initialScore, newScore);
        }

        foreach (var (address, _, newScore) in scores)
        {
            string participantKey = string.Format(
                GroupRankingRepository.ParticipantKeyFormat,
                address.ToHex()
            );
            string groupKey = string.Format(
                GroupRankingRepository.GroupKeyFormat,
                seasonId,
                roundId,
                newScore
            );

            // 새 점수 그룹에 참가자 데이터가 정확히 저장되었는지 확인
            var groupData = await Database.HashGetAsync(groupKey, participantKey);
            Assert.Equal(newScore.ToString(), groupData);

            // 새 점수 그룹이 랭킹에 정확히 반영되었는지 확인
            double? groupRankingScore = await Database.SortedSetScoreAsync(
                groupRankingKey,
                groupKey
            );
            Assert.Equal(newScore, groupRankingScore);

            // 기존 점수 그룹에 데이터가 제거되었는지 확인
            string oldGroupKey = string.Format(
                GroupRankingRepository.GroupKeyFormat,
                seasonId,
                roundId,
                newScore - 1 // 예제에서는 이전 그룹 삭제 여부를 확인
            );
            var oldGroupData = await Database.HashGetAsync(oldGroupKey, participantKey);
            Assert.True(oldGroupData.IsNullOrEmpty);
        }
    }
}

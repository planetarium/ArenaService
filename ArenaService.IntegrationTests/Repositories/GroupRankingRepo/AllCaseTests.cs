using ArenaService.IntegrationTests.Fixtures;
using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.IntegrationTests.Repositories.GroupRankingRepo;

public class AllCaseTests : BaseTest
{
    public AllCaseTests(RedisTestFixture fixture)
        : base(fixture, databaseNumber: 1) { }

    [Fact]
    public async Task AllCaseCorrectly()
    {
        var seasonId = 1;
        var roundId = 1;

        string groupRankingKey = string.Format(
            GroupRankingRepository.GroupedRankingKeyFormat,
            seasonId,
            roundId
        );

        // 테스트 데이터 설정: 고정된 점수 시나리오
        var scores = new List<(Address Address, int InitialScore, int NewScore)>
        {
            // 같은 그룹에 여러 명이 있다가 하나가 다른 그룹으로 이동
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1001),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1000),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1000, 1000),
            // 완전히 새로운 그룹이 생성되는 경우
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 2000, 3000),
            // 기존 그룹에서 데이터가 제거되며 그룹이 사라지는 경우
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1500, 1600),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1600, 1600),
            (new Address(TestUtils.GetRandomBytes(Address.Size)), 1600, 1500),
        };

        // 초기 점수 설정
        foreach (var (address, initialScore, _) in scores)
        {
            await Repository.UpdateScoreAsync(address, seasonId, roundId, 0, initialScore);
        }

        // 점수 업데이트
        foreach (var (address, initialScore, newScore) in scores)
        {
            await Repository.UpdateScoreAsync(address, seasonId, roundId, initialScore, newScore);
        }

        // 검증
        foreach (var (address, _, newScore) in scores)
        {
            string participantKey = string.Format(
                GroupRankingRepository.ParticipantKeyFormat,
                address.ToHex()
            );
            string newGroupKey = string.Format(
                GroupRankingRepository.GroupKeyFormat,
                seasonId,
                roundId,
                newScore
            );

            // 새 점수 그룹에 참가자 데이터가 정확히 저장되었는지 확인
            var groupData = await Database.HashGetAsync(newGroupKey, participantKey);
            Assert.Equal(newScore.ToString(), groupData);

            // 새 점수 그룹이 랭킹에 정확히 반영되었는지 확인
            double? newGroupRankingScore = await Database.SortedSetScoreAsync(
                groupRankingKey,
                newGroupKey
            );
            Assert.Equal(newScore, newGroupRankingScore);
        }
    }
}

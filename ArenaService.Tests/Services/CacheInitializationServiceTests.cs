namespace ArenaService.Tests.Services;

using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Moq;
using Xunit;

public class CacheInitializationServiceTests
{
    private readonly Mock<IRankingRepository> _rankingRepoMock;
    private readonly Mock<IRankingSnapshotRepository> _rankingSnapshotRepoMock;
    private readonly Mock<IParticipantRepository> _participantRepoMock;
    private readonly Mock<ISeasonCacheRepository> _seasonCacheRepoMock;
    private readonly Mock<IBlockTrackerRepository> _blockTrackerRepoMock;
    private readonly ICacheInitializationService _service;

    public CacheInitializationServiceTests()
    {
        _rankingRepoMock = new Mock<IRankingRepository>();
        _rankingSnapshotRepoMock = new Mock<IRankingSnapshotRepository>();
        _participantRepoMock = new Mock<IParticipantRepository>();
        _seasonCacheRepoMock = new Mock<ISeasonCacheRepository>();
        _blockTrackerRepoMock = new Mock<IBlockTrackerRepository>();
        _service = new CacheInitializationService(
            _rankingRepoMock.Object,
            _rankingSnapshotRepoMock.Object,
            _participantRepoMock.Object,
            _seasonCacheRepoMock.Object,
            _blockTrackerRepoMock.Object
        );
    }

    [Fact]
    public async Task InitializeRankingCacheAsync_WhenSnapshotCountSufficient_ShouldDeleteAllCachesAndReturnTrue()
    {
        // Arrange
        const int seasonId = 1;
        const int roundId = 1;
        const int participantCount = 100;
        const int snapshotCount = 60; // >= participantCount - 50 (50)

        _rankingSnapshotRepoMock
            .Setup(x => x.GetRankingSnapshotsCount(seasonId, roundId, It.IsAny<Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>?>()))
            .ReturnsAsync(snapshotCount);
        _participantRepoMock
            .Setup(x => x.GetParticipantCountAsync(seasonId))
            .ReturnsAsync(participantCount);
        _rankingRepoMock
            .Setup(x => x.ClearAllRankingCacheAsync())
            .Returns(Task.CompletedTask);
        _seasonCacheRepoMock
            .Setup(x => x.DeleteAllAsync())
            .Returns(Task.CompletedTask);
        _blockTrackerRepoMock
            .Setup(x => x.DeleteBattleTxTrackerBlockIndexAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.InitializeRankingCacheAsync(seasonId, roundId);

        // Assert
        Assert.True(result);
        _rankingRepoMock.Verify(x => x.ClearAllRankingCacheAsync(), Times.Once);
        _seasonCacheRepoMock.Verify(x => x.DeleteAllAsync(), Times.Once);
        _blockTrackerRepoMock.Verify(x => x.DeleteBattleTxTrackerBlockIndexAsync(), Times.Once);
    }

    [Fact]
    public async Task InitializeRankingCacheAsync_WhenSnapshotCountInsufficient_ShouldNotDeleteCachesAndReturnFalse()
    {
        // Arrange
        const int seasonId = 1;
        const int roundId = 1;
        const int participantCount = 100;
        const int snapshotCount = 40; // < participantCount - 50 (50)

        _rankingSnapshotRepoMock
            .Setup(x => x.GetRankingSnapshotsCount(seasonId, roundId, It.IsAny<Func<IQueryable<RankingSnapshot>, IQueryable<RankingSnapshot>>?>()))
            .ReturnsAsync(snapshotCount);
        _participantRepoMock
            .Setup(x => x.GetParticipantCountAsync(seasonId))
            .ReturnsAsync(participantCount);

        // Act
        var result = await _service.InitializeRankingCacheAsync(seasonId, roundId);

        // Assert
        Assert.False(result);
        _rankingRepoMock.Verify(x => x.ClearAllRankingCacheAsync(), Times.Never);
        _seasonCacheRepoMock.Verify(x => x.DeleteAllAsync(), Times.Never);
        _blockTrackerRepoMock.Verify(x => x.DeleteBattleTxTrackerBlockIndexAsync(), Times.Never);
    }
}

using ArenaService.Controllers;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ArenaService.Tests.Controllers;

public class CachedBlockInfoControllerTests
{
    private readonly Mock<ISeasonCacheRepository> _seasonCacheRepoMock;
    private readonly Mock<IBlockTrackerRepository> _blockTrackerRepoMock;
    private readonly CachedBlockInfoController _controller;

    public CachedBlockInfoControllerTests()
    {
        _seasonCacheRepoMock = new Mock<ISeasonCacheRepository>();
        _blockTrackerRepoMock = new Mock<IBlockTrackerRepository>();
        _controller = new CachedBlockInfoController(_seasonCacheRepoMock.Object, _blockTrackerRepoMock.Object);
    }

    [Fact]
    public async Task GetCachedBlockInfo_ShouldReturnCorrectInfo()
    {
        const long currentBlockIndex = 1000;
        const int seasonId = 1;
        const long seasonStartBlock = 900;
        const long seasonEndBlock = 1100;
        const int roundId = 1;
        const int roundIndex = 1;
        const long roundStartBlock = 950;
        const long roundEndBlock = 1050;
        const long battleTxTrackerBlock = 990;

        _seasonCacheRepoMock
            .Setup(x => x.GetBlockIndexAsync())
            .ReturnsAsync(currentBlockIndex);
        _seasonCacheRepoMock
            .Setup(x => x.GetSeasonAsync())
            .ReturnsAsync((seasonId, seasonStartBlock, seasonEndBlock));
        _seasonCacheRepoMock
            .Setup(x => x.GetRoundAsync())
            .ReturnsAsync((roundId, roundIndex, roundStartBlock, roundEndBlock));
        _blockTrackerRepoMock
            .Setup(x => x.GetBattleTxTrackerBlockIndexAsync())
            .ReturnsAsync(battleTxTrackerBlock);

        var result = await _controller.GetCachedBlockInfo();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CachedBlockInfoResponse>(okResult.Value);

        Assert.Equal(currentBlockIndex, response.CurrentBlockIndex);
        Assert.Equal(seasonId, response.Season.Id);
        Assert.Equal(seasonStartBlock, response.Season.StartBlock);
        Assert.Equal(seasonEndBlock, response.Season.EndBlock);
        Assert.Equal(roundId, response.Round.Id);
        Assert.Equal(roundStartBlock, response.Round.StartBlock);
        Assert.Equal(roundEndBlock, response.Round.EndBlock);
        Assert.Equal(battleTxTrackerBlock, response.BattleTxTrackerBlockIndex);

        _seasonCacheRepoMock.Verify(x => x.GetBlockIndexAsync(), Times.Once);
        _seasonCacheRepoMock.Verify(x => x.GetSeasonAsync(), Times.Once);
        _seasonCacheRepoMock.Verify(x => x.GetRoundAsync(), Times.Once);
        _blockTrackerRepoMock.Verify(x => x.GetBattleTxTrackerBlockIndexAsync(), Times.Once);
    }
} 
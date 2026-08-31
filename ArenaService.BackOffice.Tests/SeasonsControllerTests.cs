using ArenaService.BackOffice.Controllers;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArenaService.BackOffice.Tests;

public class SeasonsControllerTests
{
    private readonly Mock<ISeasonRepository> _seasonRepo = new();
    private readonly Mock<ISeasonCacheRepository> _seasonCacheRepo = new();
    private readonly Mock<ISeasonBlockAdjustmentService> _blockAdjustment = new();
    private readonly Mock<ISeasonService> _seasonService = new();
    private readonly SeasonsController _controller;

    public SeasonsControllerTests()
    {
        _controller = new SeasonsController(
            _seasonRepo.Object,
            _seasonCacheRepo.Object,
            _blockAdjustment.Object,
            _seasonService.Object,
            NullLogger<SeasonsController>.Instance
        );
    }

    private static Season CreateSeason(int id = 1, long startBlock = 100, long endBlock = 199) =>
        new()
        {
            Id = id,
            SeasonGroupId = 1,
            StartBlock = startBlock,
            EndBlock = endBlock,
            ArenaType = ArenaType.SEASON,
            RoundInterval = 10,
            RequiredMedalCount = 0,
            TotalPrize = 100,
            BattleTicketPolicyId = 1,
            RefreshTicketPolicyId = 1,
            Rounds = new List<Round>()
        };

    private static T Unwrap<T>(ActionResult<ApiResponse<T>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(payload.Success);
        return payload.Data!;
    }

    [Fact]
    public async Task GetSeasons_ShouldReturnPagedResultWithTotals()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonsPagedAsync(1, 10, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(new List<Season> { CreateSeason() });
        _seasonRepo.Setup(r => r.GetTotalSeasonsCountAsync()).ReturnsAsync(25);
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(50);

        var data = Unwrap(await _controller.GetSeasons());

        Assert.Single(data.Items);
        Assert.Equal(25, data.TotalCount);
        Assert.Equal(3, data.TotalPages);
    }

    [Fact]
    public async Task GetSeasons_WhenSeasonHasNotStarted_ShouldMarkDeletable()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonsPagedAsync(1, 10, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(new List<Season> { CreateSeason(startBlock: 100), CreateSeason(id: 2, startBlock: 10, endBlock: 40) });
        _seasonRepo.Setup(r => r.GetTotalSeasonsCountAsync()).ReturnsAsync(2);
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(50);

        var data = Unwrap(await _controller.GetSeasons());

        Assert.True(data.Items[0].Deletable);
        Assert.False(data.Items[1].Deletable);
    }

    [Fact]
    public async Task GetSeasons_WhenCacheIsUnavailable_ShouldLeaveDeletableNull()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonsPagedAsync(1, 10, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(new List<Season> { CreateSeason() });
        _seasonRepo.Setup(r => r.GetTotalSeasonsCountAsync()).ReturnsAsync(1);
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ThrowsAsync(new InvalidOperationException());

        var data = Unwrap(await _controller.GetSeasons());

        Assert.Null(data.Items[0].Deletable);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetSeasons_WithNonPositivePaging_ShouldReturnBadRequest(int page, int pageSize)
    {
        var result = await _controller.GetSeasons(page, pageSize);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSeason_WhenMissing_ShouldReturnNotFound()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonAsync(99, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ThrowsAsync(new InvalidOperationException("missing"));

        var result = await _controller.GetSeason(99);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetNextStartBlock_ShouldReturnLastEndBlockPlusOne()
    {
        _seasonRepo.Setup(r => r.GetLastSeasonEndBlockAsync()).ReturnsAsync(199);

        Assert.Equal(200, Unwrap(await _controller.GetNextStartBlock()));
    }

    [Fact]
    public async Task GetNextStartBlock_WhenNoSeasonExists_ShouldReturnOne()
    {
        _seasonRepo.Setup(r => r.GetLastSeasonEndBlockAsync()).ReturnsAsync((int?)null);

        Assert.Equal(1, Unwrap(await _controller.GetNextStartBlock()));
    }

    [Fact]
    public async Task AddSeason_WhenBlockRangeOverlaps_ShouldReturnConflict()
    {
        _seasonRepo.Setup(r => r.IsBlockRangeOverlappingAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);

        var result = await _controller.AddSeason(
            new AddSeasonRequest
            {
                StartBlock = 100,
                RoundInterval = 10,
                RoundCount = 10,
                BattleTicketPolicyId = 1,
                RefreshTicketPolicyId = 1
            }
        );

        Assert.IsType<ConflictObjectResult>(result.Result);
        _seasonRepo.Verify(
            r => r.AddSeasonWithRoundsAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ArenaType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task AddSeason_ShouldComputeEndBlockWithoutIntOverflow()
    {
        // roundInterval * roundCount overflows int; the check must run on the widened value.
        long? capturedEndBlock = null;
        _seasonRepo
            .Setup(r => r.IsBlockRangeOverlappingAsync(It.IsAny<long>(), It.IsAny<long>()))
            .Callback<long, long>((_, endBlock) => capturedEndBlock = endBlock)
            .ReturnsAsync(false);
        _seasonRepo
            .Setup(r => r.AddSeasonWithRoundsAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<ArenaType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(CreateSeason());

        await _controller.AddSeason(
            new AddSeasonRequest
            {
                StartBlock = 1,
                RoundInterval = 100_000,
                RoundCount = 100_000,
                BattleTicketPolicyId = 1,
                RefreshTicketPolicyId = 1
            }
        );

        Assert.Equal(10_000_000_000L, capturedEndBlock);
    }

    [Fact]
    public async Task DeleteSeason_WhenSeasonAlreadyStarted_ShouldReturnConflict()
    {
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(150);
        _seasonService.Setup(s => s.CanDeleteSeasonAsync(1, 150)).ReturnsAsync(false);

        var result = await _controller.DeleteSeason(1);

        Assert.IsType<ConflictObjectResult>(result.Result);
        _seasonService.Verify(s => s.DeleteSeasonAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSeason_ShouldUseServerSideBlockIndex()
    {
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(42);
        _seasonService.Setup(s => s.CanDeleteSeasonAsync(1, 42)).ReturnsAsync(true);

        var result = await _controller.DeleteSeason(1);

        Assert.IsType<OkObjectResult>(result.Result);
        _seasonCacheRepo.Verify(r => r.GetBlockIndexAsync(), Times.Once);
        _seasonService.Verify(s => s.DeleteSeasonAsync(1), Times.Once);
    }

    [Fact]
    public async Task AdjustEndBlock_ShouldDelegateToService()
    {
        _blockAdjustment
            .Setup(s => s.AdjustSeasonEndBlockAsync(1, 500))
            .ReturnsAsync(CreateSeason(endBlock: 500));

        var data = Unwrap(await _controller.AdjustEndBlock(1, new AdjustEndBlockRequest { NewEndBlock = 500 }));

        Assert.Equal(500, data.EndBlock);
    }
}

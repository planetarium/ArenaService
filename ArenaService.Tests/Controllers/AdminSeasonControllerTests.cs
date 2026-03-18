using ArenaService.Controllers;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class AdminSeasonControllerTests
{
    private readonly AdminSeasonController _controller;
    private readonly Mock<ISeasonRepository> _seasonRepoMock;
    private readonly Mock<ISeasonService> _seasonServiceMock;
    private readonly Mock<ISeasonBlockAdjustmentService> _adjustmentServiceMock;
    private readonly Mock<ISeasonCacheRepository> _seasonCacheRepoMock;

    public AdminSeasonControllerTests()
    {
        _seasonRepoMock = new Mock<ISeasonRepository>();
        _seasonServiceMock = new Mock<ISeasonService>();
        _adjustmentServiceMock = new Mock<ISeasonBlockAdjustmentService>();
        _seasonCacheRepoMock = new Mock<ISeasonCacheRepository>();

        _controller = new AdminSeasonController(
            _seasonRepoMock.Object,
            _seasonServiceMock.Object,
            _adjustmentServiceMock.Object,
            _seasonCacheRepoMock.Object
        );
    }

    private static Season CreateTestSeason(int id = 1) => new Season
    {
        Id = id,
        SeasonGroupId = 1,
        StartBlock = 1000,
        EndBlock = 2000,
        ArenaType = ArenaType.SEASON,
        RoundInterval = 100,
        RequiredMedalCount = 0,
        TotalPrize = 1000,
        BattleTicketPolicyId = 1,
        RefreshTicketPolicyId = 1,
        BattleTicketPolicy = new BattleTicketPolicy { Id = 1, Name = "Test" },
        RefreshTicketPolicy = new RefreshTicketPolicy { Id = 1, Name = "Test" },
        Rounds = new List<Round>()
    };

    [Fact]
    public async Task CreateSeason_ValidRequest_ReturnsCreated()
    {
        var season = CreateTestSeason();
        _seasonRepoMock.Setup(x => x.AddSeasonWithRoundsAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<ArenaType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()
        )).ReturnsAsync(season);

        var request = new CreateSeasonRequest
        {
            StartBlock = 1000,
            RoundInterval = 100,
            RoundCount = 10,
            SeasonGroupId = 1,
            ArenaType = ArenaType.SEASON,
            RequiredMedalCount = 0,
            TotalPrize = 1000,
            BattleTicketPolicyId = 1,
            RefreshTicketPolicyId = 1
        };

        var result = await _controller.CreateSeason(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(season, createdResult.Value);
    }

    [Fact]
    public async Task CreateSeason_OverlappingBlocks_ReturnsConflict()
    {
        _seasonRepoMock.Setup(x => x.AddSeasonWithRoundsAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<ArenaType>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()
        )).ThrowsAsync(new InvalidOperationException());

        var request = new CreateSeasonRequest
        {
            StartBlock = 1000,
            RoundInterval = 100,
            RoundCount = 10,
            SeasonGroupId = 1,
            ArenaType = ArenaType.SEASON,
        };

        var result = await _controller.CreateSeason(request);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GetSeason_Exists_ReturnsOk()
    {
        var season = CreateTestSeason();
        _seasonRepoMock.Setup(x => x.GetSeasonAsync(
            1, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()
        )).ReturnsAsync(season);

        var result = await _controller.GetSeason(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(season, okResult.Value);
    }

    [Fact]
    public async Task GetSeason_NotFound_ReturnsNotFound()
    {
        _seasonRepoMock.Setup(x => x.GetSeasonAsync(
            999, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()
        )).ThrowsAsync(new InvalidOperationException());

        var result = await _controller.GetSeason(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateSeason_ValidRequest_ReturnsOk()
    {
        var season = CreateTestSeason();
        _seasonRepoMock.Setup(x => x.UpdateSeasonAsync(
            1, It.IsAny<int>(), It.IsAny<ArenaType>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()
        )).ReturnsAsync(season);

        var request = new UpdateSeasonRequest
        {
            SeasonGroupId = 1,
            ArenaType = ArenaType.SEASON,
            RoundInterval = 100,
            RequiredMedalCount = 0,
            TotalPrize = 2000,
            BattleTicketPolicyId = 1,
            RefreshTicketPolicyId = 1
        };

        var result = await _controller.UpdateSeason(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(season, okResult.Value);
    }

    [Fact]
    public async Task DeleteSeason_CanDelete_ReturnsNoContent()
    {
        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(500);
        _seasonServiceMock.Setup(x => x.CanDeleteSeasonAsync(1, 500)).ReturnsAsync(true);

        var result = await _controller.DeleteSeason(1);

        Assert.IsType<NoContentResult>(result);
        _seasonServiceMock.Verify(x => x.DeleteSeasonAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteSeason_AlreadyStarted_ReturnsBadRequest()
    {
        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(1500);
        _seasonServiceMock.Setup(x => x.CanDeleteSeasonAsync(1, 1500)).ReturnsAsync(false);

        var result = await _controller.DeleteSeason(1);

        Assert.IsType<BadRequestObjectResult>(result);
        _seasonServiceMock.Verify(x => x.DeleteSeasonAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AdjustEndBlock_ValidRequest_ReturnsOk()
    {
        var season = CreateTestSeason();
        season.EndBlock = 3000;
        _adjustmentServiceMock.Setup(x => x.AdjustSeasonEndBlockAsync(1, 3000))
            .ReturnsAsync(season);

        var request = new AdjustEndBlockRequest { NewEndBlock = 3000 };

        var result = await _controller.AdjustEndBlock(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedSeason = Assert.IsType<Season>(okResult.Value);
        Assert.Equal(3000, returnedSeason.EndBlock);
    }
}

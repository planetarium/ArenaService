using ArenaService.Controllers;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class AdminPreparationControllerTests
{
    private readonly AdminPreparationController _controller;
    private readonly Mock<ISeasonPreparationService> _seasonPreparationMock;
    private readonly Mock<IRoundPreparationService> _roundPreparationMock;
    private readonly Mock<ICacheInitializationService> _cacheInitMock;
    private readonly Mock<ISeasonService> _seasonServiceMock;

    public AdminPreparationControllerTests()
    {
        _seasonPreparationMock = new Mock<ISeasonPreparationService>();
        _roundPreparationMock = new Mock<IRoundPreparationService>();
        _cacheInitMock = new Mock<ICacheInitializationService>();
        _seasonServiceMock = new Mock<ISeasonService>();

        _controller = new AdminPreparationController(
            _seasonPreparationMock.Object,
            _roundPreparationMock.Object,
            _cacheInitMock.Object,
            _seasonServiceMock.Object
        );
    }

    private static (Season Season, Round Round) CreateTestSeasonAndRound() =>
    (
        new Season
        {
            Id = 1,
            SeasonGroupId = 1,
            StartBlock = 1000,
            EndBlock = 2000,
            ArenaType = ArenaType.SEASON,
            RoundInterval = 100,
            BattleTicketPolicyId = 1,
            RefreshTicketPolicyId = 1,
            BattleTicketPolicy = new BattleTicketPolicy { Id = 1, Name = "Test" },
            RefreshTicketPolicy = new RefreshTicketPolicy { Id = 1, Name = "Test" },
            Rounds = new List<Round>()
        },
        new Round
        {
            Id = 1,
            SeasonId = 1,
            StartBlock = 1000,
            EndBlock = 1099,
            RoundIndex = 1
        }
    );

    [Fact]
    public async Task InitializeSeason_ValidBlock_ReturnsOk()
    {
        var seasonAndRound = CreateTestSeasonAndRound();
        _seasonServiceMock.Setup(x => x.GetSeasonAndRoundByBlock(1050))
            .ReturnsAsync(seasonAndRound);

        var result = await _controller.InitializeSeason(new InitializeSeasonRequest { BlockIndex = 1050 });

        Assert.IsType<OkObjectResult>(result);
        _seasonPreparationMock.Verify(x => x.PrepareSeasonAsync(seasonAndRound), Times.Once);
    }

    [Fact]
    public async Task InitializeSeason_InvalidBlock_ReturnsNotFound()
    {
        _seasonServiceMock.Setup(x => x.GetSeasonAndRoundByBlock(99999))
            .ThrowsAsync(new NotFoundSeasonException("No matching season found"));

        var result = await _controller.InitializeSeason(new InitializeSeasonRequest { BlockIndex = 99999 });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PrepareNextRound_ValidBlock_ReturnsOk()
    {
        var seasonAndRound = CreateTestSeasonAndRound();
        _seasonServiceMock.Setup(x => x.GetSeasonAndRoundByBlock(1050))
            .ReturnsAsync(seasonAndRound);

        var result = await _controller.PrepareNextRound(new PrepareNextRoundRequest { BlockIndex = 1050 });

        Assert.IsType<OkObjectResult>(result);
        _roundPreparationMock.Verify(
            x => x.PrepareNextRoundWithSnapshotAsync(seasonAndRound), Times.Once
        );
    }

    [Fact]
    public async Task InitializeRankingCache_Successful_ReturnsOk()
    {
        _cacheInitMock.Setup(x => x.InitializeRankingCacheAsync(1, 1)).ReturnsAsync(true);

        var result = await _controller.InitializeRankingCache(
            new InitializeRankingCacheRequest { SeasonId = 1, RoundId = 1 }
        );

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task InitializeRankingCache_InsufficientSnapshots_ReturnsBadRequest()
    {
        _cacheInitMock.Setup(x => x.InitializeRankingCacheAsync(1, 1)).ReturnsAsync(false);

        var result = await _controller.InitializeRankingCache(
            new InitializeRankingCacheRequest { SeasonId = 1, RoundId = 1 }
        );

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

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

public class RankingCacheControllerTests
{
    private readonly Mock<ISeasonRepository> _seasonRepo = new();
    private readonly Mock<ISeasonCacheRepository> _seasonCacheRepo = new();
    private readonly Mock<IRankingRepository> _rankingRepo = new();
    private readonly Mock<IRoundRepository> _roundRepo = new();
    private readonly Mock<ISeasonPreparationService> _seasonPreparation = new();
    private readonly Mock<IRoundPreparationService> _roundPreparation = new();
    private readonly Mock<ICacheInitializationService> _cacheInitialization = new();
    private readonly RankingCacheController _controller;

    public RankingCacheControllerTests()
    {
        _controller = new RankingCacheController(
            _seasonRepo.Object,
            _seasonCacheRepo.Object,
            _rankingRepo.Object,
            _roundRepo.Object,
            _seasonPreparation.Object,
            _roundPreparation.Object,
            _cacheInitialization.Object,
            NullLogger<RankingCacheController>.Instance
        );
    }

    private static Season CreateSeason(int id = 1, params int[] roundIndexes) =>
        new()
        {
            Id = id,
            ArenaType = ArenaType.SEASON,
            Rounds = roundIndexes
                .Select(i => new Round { Id = 100 + i, SeasonId = id, RoundIndex = i })
                .ToList()
        };

    [Fact]
    public async Task GetStatus_ShouldReturnSurroundingRoundCounts()
    {
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(1234);
        _seasonCacheRepo.Setup(r => r.GetSeasonAsync()).ReturnsAsync((1, 100L, 200L));
        _seasonCacheRepo.Setup(r => r.GetRoundAsync()).ReturnsAsync((11, 3, 120L, 130L));
        _rankingRepo.Setup(r => r.GetRankingCountAsync(1, It.IsAny<int>())).ReturnsAsync(50);

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetStatus()).Result);
        var payload = Assert.IsType<ApiResponse<RankingCacheStatusDto>>(ok.Value);

        Assert.Equal(1234, payload.Data!.BlockIndex);
        Assert.Equal(new[] { 2, 3, 4 }, payload.Data!.RankingCounts.Select(c => c.RoundIndex));
    }

    [Fact]
    public async Task GetStatus_OnFirstRound_ShouldSkipRoundZero()
    {
        _seasonCacheRepo.Setup(r => r.GetBlockIndexAsync()).ReturnsAsync(100);
        _seasonCacheRepo.Setup(r => r.GetSeasonAsync()).ReturnsAsync((1, 100L, 200L));
        _seasonCacheRepo.Setup(r => r.GetRoundAsync()).ReturnsAsync((10, 1, 100L, 110L));
        _rankingRepo.Setup(r => r.GetRankingCountAsync(1, It.IsAny<int>())).ReturnsAsync(0);

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetStatus()).Result);
        var payload = Assert.IsType<ApiResponse<RankingCacheStatusDto>>(ok.Value);

        Assert.Equal(new[] { 1, 2 }, payload.Data!.RankingCounts.Select(c => c.RoundIndex));
    }

    [Fact]
    public async Task PrepareSeason_ShouldUseFirstRound()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonAsync(1, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(CreateSeason(1, 3, 1, 2));

        Assert.IsType<OkObjectResult>((await _controller.PrepareSeason(1)).Result);
        _seasonPreparation.Verify(
            s => s.PrepareSeasonAsync(It.Is<(Season Season, Round Round)>(t => t.Round.RoundIndex == 1)),
            Times.Once
        );
    }

    [Fact]
    public async Task PrepareSeason_WhenSeasonHasNoRounds_ShouldReturnBadRequest()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonAsync(1, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(CreateSeason(1));

        Assert.IsType<BadRequestObjectResult>((await _controller.PrepareSeason(1)).Result);
        _seasonPreparation.Verify(
            s => s.PrepareSeasonAsync(It.IsAny<(Season, Round)>()),
            Times.Never
        );
    }

    [Fact]
    public async Task PrepareNextRound_WhenRoundIsMissing_ShouldReturnNotFound()
    {
        _roundRepo
            .Setup(r => r.GetRoundAsync(9, It.IsAny<Func<IQueryable<Round>, IQueryable<Round>>>()))
            .ReturnsAsync((Round?)null);

        Assert.IsType<NotFoundObjectResult>((await _controller.PrepareNextRound(9)).Result);
    }

    [Fact]
    public async Task InitializeRankingCache_WhenServiceFails_ShouldReturn500()
    {
        _seasonCacheRepo.Setup(r => r.GetSeasonAsync()).ReturnsAsync((1, 100L, 200L));
        _seasonCacheRepo.Setup(r => r.GetRoundAsync()).ReturnsAsync((11, 3, 120L, 130L));
        _cacheInitialization.Setup(s => s.InitializeRankingCacheAsync(1, 11)).ReturnsAsync(false);

        var result = Assert.IsType<ObjectResult>((await _controller.InitializeRankingCache()).Result);

        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task InitializeRankingCache_WhenServiceSucceeds_ShouldReturnOk()
    {
        _seasonCacheRepo.Setup(r => r.GetSeasonAsync()).ReturnsAsync((1, 100L, 200L));
        _seasonCacheRepo.Setup(r => r.GetRoundAsync()).ReturnsAsync((11, 3, 120L, 130L));
        _cacheInitialization.Setup(s => s.InitializeRankingCacheAsync(1, 11)).ReturnsAsync(true);

        Assert.IsType<OkObjectResult>((await _controller.InitializeRankingCache()).Result);
    }
}

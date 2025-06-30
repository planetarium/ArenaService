using System.Collections.Generic;
using ArenaService.Controllers;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class LeaderboardControllerTests
{
    private readonly Mock<IAllClanRankingRepository> _mockAllClanRankingRepo;
    private readonly Mock<IRankingRepository> _mockRankingRepo;
    private readonly Mock<ILeaderboardRepository> _mockLeaderboardRepo;
    private readonly Mock<ISeasonService> _mockSeasonService;
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _mockAllClanRankingRepo = new Mock<IAllClanRankingRepository>();
        _mockRankingRepo = new Mock<IRankingRepository>();
        _mockLeaderboardRepo = new Mock<ILeaderboardRepository>();
        _mockSeasonService = new Mock<ISeasonService>();

        _controller = new LeaderboardController(
            _mockAllClanRankingRepo.Object,
            _mockRankingRepo.Object,
            _mockLeaderboardRepo.Object,
            _mockSeasonService.Object
        );
    }

    [Fact]
    public async Task GetCompletedArenaLeaderboard_WithValidBlockIndex_ReturnsOkResult()
    {
        // Arrange
        var blockIndex = 1000000L;
        var season = new Season
        {
            Id = 1,
            SeasonGroupId = 1,
            StartBlock = 400000,
            EndBlock = 800000,
            ArenaType = ArenaType.SEASON,
        };
        var round = new Round
        {
            Id = 1,
            RoundIndex = 1,
            StartBlock = 400000,
            EndBlock = 800000,
        };

        var user = new User
        {
            AvatarAddress = new Address("0x1234567890123456789012345678901234567890"),
            AgentAddress = new Address("0x0987654321098765432109876543210987654321"),
            NameWithHash = "TestUser#123",
            Level = 10,
        };

        var participant = new Participant
        {
            User = user,
            TotalWin = 10,
            TotalLose = 5,
        };

        var leaderboardData = new List<(Participant Participant, int Score, int Rank)>
        {
            (participant, 100, 1),
        };

        _mockSeasonService
            .Setup(x => x.GetSeasonAndRoundByBlock(blockIndex))
            .ReturnsAsync((season, round));

        _mockLeaderboardRepo
            .Setup(x => x.GetLeaderboardAsync(season.Id))
            .ReturnsAsync(leaderboardData);

        // Act
        var result = await _controller.GetCompletedArenaLeaderboard(blockIndex);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CompletedSeasonLeaderboardResponse>(okResult.Value);
        
        Assert.Equal(season.Id, response.Season.Id);
        Assert.Single(response.Leaderboard);
        
        var leaderboardEntry = response.Leaderboard[0];
        Assert.Equal(1, leaderboardEntry.Rank);
        Assert.Equal(100, leaderboardEntry.Score);
        Assert.Equal("0x1234567890123456789012345678901234567890", leaderboardEntry.AvatarAddress);
        Assert.Equal("0x0987654321098765432109876543210987654321", leaderboardEntry.AgentAddress);
        Assert.Equal("TestUser#123", leaderboardEntry.NameWithHash);
        Assert.Equal(10, leaderboardEntry.TotalWin);
        Assert.Equal(5, leaderboardEntry.TotalLose);
        Assert.Equal(10, leaderboardEntry.Level);
    }

    [Fact]
    public async Task GetCompletedArenaLeaderboard_WithOngoingSeason_ReturnsBadRequest()
    {
        // Arrange
        var blockIndex = 600000L;
        var season = new Season
        {
            Id = 1,
            SeasonGroupId = 1,
            StartBlock = 400000,
            EndBlock = 800000,
            ArenaType = ArenaType.SEASON,
        };
        var round = new Round
        {
            Id = 1,
            RoundIndex = 1,
            StartBlock = 400000,
            EndBlock = 800000,
        };

        _mockSeasonService
            .Setup(x => x.GetSeasonAndRoundByBlock(blockIndex))
            .ReturnsAsync((season, round));

        // Act
        var result = await _controller.GetCompletedArenaLeaderboard(blockIndex);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("The requested block index corresponds to an ongoing season.", badRequestResult.Value);
    }

    [Fact]
    public async Task GetCompletedArenaLeaderboard_WithException_ReturnsBadRequest()
    {
        // Arrange
        var blockIndex = 1000000L;
        var errorMessage = "Season not found";

        _mockSeasonService
            .Setup(x => x.GetSeasonAndRoundByBlock(blockIndex))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _controller.GetCompletedArenaLeaderboard(blockIndex);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }
}

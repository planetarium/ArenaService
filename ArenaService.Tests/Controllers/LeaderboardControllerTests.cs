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
    private readonly Mock<ISeasonCacheRepository> _mockSeasonCacheRepo;
    private readonly Mock<ISeasonRepository> _mockSeasonRepo;
    private readonly Mock<IRankingSnapshotRepository> _mockRankingSnapshotRepo;
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _mockAllClanRankingRepo = new Mock<IAllClanRankingRepository>();
        _mockRankingRepo = new Mock<IRankingRepository>();
        _mockLeaderboardRepo = new Mock<ILeaderboardRepository>();
        _mockSeasonService = new Mock<ISeasonService>();
        _mockSeasonCacheRepo = new Mock<ISeasonCacheRepository>();
        _mockSeasonRepo = new Mock<ISeasonRepository>();
        _mockRankingSnapshotRepo = new Mock<IRankingSnapshotRepository>();

        _controller = new LeaderboardController(
            _mockAllClanRankingRepo.Object,
            _mockRankingRepo.Object,
            _mockLeaderboardRepo.Object,
            _mockSeasonService.Object,
            _mockSeasonCacheRepo.Object,
            _mockSeasonRepo.Object,
            _mockRankingSnapshotRepo.Object
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

        var currentSeasonInfo = (Id: 2, StartBlock: 1200000L, EndBlock: 1600000L);

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

        _mockSeasonRepo
            .Setup(x => x.GetSeasonAsync(season.Id, null))
            .ReturnsAsync(season);

        _mockSeasonCacheRepo
            .Setup(x => x.GetSeasonAsync())
            .ReturnsAsync(currentSeasonInfo);

        _mockLeaderboardRepo
            .Setup(x => x.GetLeaderboardAsync(season.Id))
            .ReturnsAsync(leaderboardData);

        // Act
        var result = await _controller.GetCompletedArenaLeaderboard(1);

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

        var currentSeasonInfo = (Id: 1, StartBlock: 400000L, EndBlock: 800000L);

        _mockSeasonService
            .Setup(x => x.GetSeasonAndRoundByBlock(blockIndex))
            .ReturnsAsync((season, round));

        _mockSeasonCacheRepo
            .Setup(x => x.GetSeasonAsync())
            .ReturnsAsync(currentSeasonInfo);

        // Act
        var result = await _controller.GetCompletedArenaLeaderboard(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
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
        var result = await _controller.GetCompletedArenaLeaderboard(1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRankingSnapshot_RespectsPaginationParameters()
    {
        // Arrange
        int seasonId = 1;
        int roundId = 1;
        var entries = Enumerable.Range(0, 1500).Select(i => new RankingSnapshotEntryResponse
        {
            AgentAddress = new Address($"0x{i.ToString("x").PadLeft(40, '0')}"),
            AvatarAddress = new Address($"0x{(i + 10000).ToString("x").PadLeft(40, '0')}"),
            NameWithHash = $"Player#{i}",
            Level = i,
            Cp = i * 100,
            Score = 2000 - i
        }).ToList();

        _mockRankingSnapshotRepo
            .Setup(x => x.GetRankingSnapshotEntries(seasonId, roundId, 0, 1000))
            .ReturnsAsync(entries.Take(1000).ToList());
        _mockRankingSnapshotRepo
            .Setup(x => x.GetRankingSnapshotEntries(seasonId, roundId, 1000, 1000))
            .ReturnsAsync(entries.Skip(1000).Take(1000).ToList());
        _mockRankingSnapshotRepo
            .Setup(x => x.GetRankingSnapshotEntries(seasonId, roundId, 500, 200))
            .ReturnsAsync(entries.Skip(500).Take(200).ToList());

        // Act
        var firstPageResult = await _controller.GetRankingSnapshot(seasonId, roundId, 0, 1000);
        var secondPageResult = await _controller.GetRankingSnapshot(seasonId, roundId, 1000, 1000);
        var customPageResult = await _controller.GetRankingSnapshot(seasonId, roundId, 500, 200);

        // Assert
        var firstPage = Assert.IsType<OkObjectResult>(firstPageResult.Result);
        var secondPage = Assert.IsType<OkObjectResult>(secondPageResult.Result);
        var customPage = Assert.IsType<OkObjectResult>(customPageResult.Result);

        var firstPageEntries = Assert.IsType<List<RankingSnapshotEntryResponse>>(firstPage.Value);
        var secondPageEntries = Assert.IsType<List<RankingSnapshotEntryResponse>>(secondPage.Value);
        var customPageEntries = Assert.IsType<List<RankingSnapshotEntryResponse>>(customPage.Value);

        Assert.Equal(1000, firstPageEntries.Count);
        Assert.Equal(500, secondPageEntries.Count);
        Assert.Equal(200, customPageEntries.Count);
        Assert.Equal(entries[0].AvatarAddress, firstPageEntries.First().AvatarAddress);
        Assert.Equal(entries[1000].AvatarAddress, secondPageEntries.First().AvatarAddress);
        Assert.Equal(entries[500].AvatarAddress, customPageEntries.First().AvatarAddress);
    }
}

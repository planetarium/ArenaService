using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class SeasonControllerTests
{
    private readonly SeasonController _controller;
    private Mock<ISeasonRepository> _repositoryMock;
    private SeasonService _service;

    public SeasonControllerTests()
    {
        var repositoryMock = new Mock<ISeasonRepository>();
        _repositoryMock = repositoryMock;
        _service = new SeasonService(repositoryMock.Object);
        _controller = new SeasonController(_service);
    }

    [Fact]
    public async Task GetCurrentSeason_ActivatedSeasonsExist_ReturnsOkWithCorrectSeason()
    {
        var blockIndex = 1000;
        var season = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 700,
                EndBlockIndex = 899,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            },
            new Season
            {
                Id = 3,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            }
        };

        _repositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(season);

        var result = await _controller.GetCurrentSeason(blockIndex);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<SeasonResponse>(okResult.Value);

        Assert.Equal(season[1].Id, returnValue.Id);
    }

    [Fact]
    public async Task GetCurrentSeason_MultipleSeasonsSameBlockIndex_ReturnsFirstMatchingSeason()
    {
        var blockIndex = 1000;
        var season = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            },
            new Season
            {
                Id = 3,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            }
        };

        _repositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(season);

        var result = await _controller.GetCurrentSeason(blockIndex);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<SeasonResponse>(okResult.Value);

        Assert.Equal(season[0].Id, returnValue.Id);
    }

    [Fact]
    public async Task GetCurrentSeason_NoMatchingSeasonExists_ReturnsNotFound()
    {
        var blockIndex = 1101;

        var season = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 700,
                EndBlockIndex = 899,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            },
            new Season
            {
                Id = 3,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                BattleLogs = new List<BattleLog>(),
                Leaderboard = new List<LeaderboardEntry>()
            }
        };

        _repositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(season);

        var result = await _controller.GetCurrentSeason(blockIndex);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetCurrentSeason_NoActivatedSeasons_ReturnsNotFound()
    {
        var blockIndex = 1000;

        _repositoryMock
            .Setup(repo => repo.GetActivatedSeasonsAsync())
            .ReturnsAsync(new List<Season>());

        var result = await _controller.GetCurrentSeason(blockIndex);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}

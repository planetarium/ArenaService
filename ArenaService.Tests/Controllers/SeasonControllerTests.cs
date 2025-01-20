using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace ArenaService.Tests.Controllers;

public class SeasonControllerTests
{
    private readonly SeasonController _controller;
    private Mock<ISeasonRepository> _repoMock;

    public SeasonControllerTests()
    {
        var repoMock = new Mock<ISeasonRepository>();
        _repoMock = repoMock;
        _controller = new SeasonController(_repoMock.Object);
    }

    [Fact]
    public async Task GetCurrentSeason_ActivatedSeasonsExist_ReturnsOkWithCorrectSeason()
    {
        var blockIndex = 1000;
        var seasons = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 700,
                EndBlockIndex = 899,
                TicketRefillInterval = 600,
                IsActivated = true
            },
            new Season
            {
                Id = 3,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true
            }
        };

        _repoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(seasons);

        var result = await _controller.GetCurrentSeason(blockIndex);

        var okResult = Assert.IsType<Ok<SeasonResponse>>(result.Result);
        var returnValue = Assert.IsType<SeasonResponse>(okResult.Value);

        Assert.Equal(seasons[1].Id, returnValue.Id);
    }

    [Fact]
    public async Task GetCurrentSeason_MultipleSeasonsSameBlockIndex_ReturnsFirstMatchingSeason()
    {
        var blockIndex = 1000;
        var seasons = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 900,
                EndBlockIndex = 1100,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                Battles = new List<Battle>(),
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
                Battles = new List<Battle>(),
                Leaderboard = new List<LeaderboardEntry>()
            }
        };

        _repoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(seasons);

        var result = await _controller.GetCurrentSeason(blockIndex);

        var okResult = Assert.IsType<Ok<SeasonResponse>>(result.Result);
        var returnValue = Assert.IsType<SeasonResponse>(okResult.Value);

        Assert.Equal(seasons[0].Id, returnValue.Id);
    }

    [Fact]
    public async Task GetCurrentSeason_NoMatchingSeasonExists_ReturnsNotFound()
    {
        var blockIndex = 1101;

        var seasons = new List<Season>()
        {
            new Season
            {
                Id = 2,
                StartBlockIndex = 700,
                EndBlockIndex = 899,
                TicketRefillInterval = 600,
                IsActivated = true,
                Participants = new List<Participant>(),
                Battles = new List<Battle>(),
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
                Battles = new List<Battle>(),
                Leaderboard = new List<LeaderboardEntry>()
            }
        };

        _repoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(seasons);

        var result = await _controller.GetCurrentSeason(blockIndex);

        Assert.IsType<NotFound<string>>(result.Result);
    }

    [Fact]
    public async Task GetCurrentSeason_NoActivatedSeasons_ReturnsNotFound()
    {
        var blockIndex = 1000;

        _repoMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync(new List<Season>());

        var result = await _controller.GetCurrentSeason(blockIndex);

        Assert.IsType<NotFound<string>>(result.Result);
    }
}

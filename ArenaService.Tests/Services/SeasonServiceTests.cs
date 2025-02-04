namespace ArenaService.Tests.Services;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArenaService.Constants;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using Moq;
using Xunit;

public class SeasonServiceTests
{
    private readonly Mock<ISeasonRepository> _seasonRepoMock;
    private readonly ISeasonService _seasonService;

    public SeasonServiceTests()
    {
        _seasonRepoMock = new Mock<ISeasonRepository>();
        _seasonService = new SeasonService(_seasonRepoMock.Object);
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsCurrentSeason_WhenInSingleChampionship()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 100,
                EndBlock = 200
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(150);

        // Assert
        Assert.NotNull(result);
        Assert.Equal([1], result.Select(s => s.Id).ToList());
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsEmptyList_WhenBlockIndexOutsideAllSeasons()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 100,
                EndBlock = 200
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(300);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsSeasons_BetweenPreviousAndNextChampionships()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 100,
                EndBlock = 200
            },
            new Season
            {
                Id = 2,
                ArenaType = ArenaType.SEASON,
                StartBlock = 201,
                EndBlock = 250
            },
            new Season
            {
                Id = 3,
                ArenaType = ArenaType.OFF_SEASON,
                StartBlock = 251,
                EndBlock = 300
            },
            new Season
            {
                Id = 4,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 301,
                EndBlock = 400
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(275);

        // Assert
        Assert.NotNull(result);
        Assert.Equal([2, 3, 4], result.Select(s => s.Id).ToList());
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsOnlyCurrentSeason_WhenNoPreviousOrNextChampionshipsExist()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.SEASON,
                StartBlock = 100,
                EndBlock = 200
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(150);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, s => s.Id == 1);
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsEmptyList_WhenNoChampionshipExists()
    {
        // Arrange
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.SEASON,
                StartBlock = 100,
                EndBlock = 200
            },
            new Season
            {
                Id = 2,
                ArenaType = ArenaType.OFF_SEASON,
                StartBlock = 201,
                EndBlock = 300
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(150);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, s => s.Id == 1);
    }

    [Fact]
    public async Task ClassifyByChampionship_ReturnsSeasons_NowChampionship()
    {
        // Arrange

        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 100,
                EndBlock = 200
            },
            new Season
            {
                Id = 2,
                ArenaType = ArenaType.SEASON,
                StartBlock = 201,
                EndBlock = 250
            },
            new Season
            {
                Id = 3,
                ArenaType = ArenaType.OFF_SEASON,
                StartBlock = 251,
                EndBlock = 300
            },
            new Season
            {
                Id = 4,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 301,
                EndBlock = 400
            },
            new Season
            {
                Id = 5,
                ArenaType = ArenaType.SEASON,
                StartBlock = 401,
                EndBlock = 450
            },
            new Season
            {
                Id = 6,
                ArenaType = ArenaType.OFF_SEASON,
                StartBlock = 451,
                EndBlock = 500
            },
            new Season
            {
                Id = 7,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 501,
                EndBlock = 550
            }
        };

        _seasonRepoMock
            .Setup(repo =>
                repo.GetAllSeasonsAsync(It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>())
            )
            .ReturnsAsync(seasons);

        // Act
        var result = await _seasonService.ClassifyByChampionship(302);

        // Assert
        Assert.NotNull(result);
        Assert.Equal([2, 3, 4], result.Select(s => s.Id).ToList());
    }
}

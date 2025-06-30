using ArenaService.Controllers;
using ArenaService.Shared.Constants;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Models.Ticket;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using ArenaService.Options;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace ArenaService.Tests.Controllers;

public class SeasonControllerTests
{
    private readonly SeasonController _controller;
    private readonly Mock<ISeasonRepository> _repoMock;
    private readonly Mock<ISeasonService> _serviceMock;
    private readonly Address _recipientAddress;

    public SeasonControllerTests()
    {
        _repoMock = new Mock<ISeasonRepository>();
        _serviceMock = new Mock<ISeasonService>();
        _recipientAddress = new Address("0x0000000000000000000000000000000000000000");
        
        var options = new Mock<IOptions<OpsConfigOptions>>();
        options.Setup(x => x.Value).Returns(new OpsConfigOptions 
        { 
            RecipientAddress = _recipientAddress.ToString(),
            JwtSecretKey = "test-secret-key",
            JwtPublicKey = "test-public-key",
            ArenaProviderName = "test-provider",
            HangfireUsername = "test-user",
            HangfirePassword = "test-password"
        });

        _controller = new SeasonController(_repoMock.Object, _serviceMock.Object, options.Object);
    }

    [Fact]
    public async Task GetSeasonsPaged_ValidParameters_ReturnsOkResult()
    {
        var seasons = new List<Season>
        {
            new Season
            {
                Id = 1,
                SeasonGroupId = 1,
                ArenaType = ArenaType.CHAMPIONSHIP,
                StartBlock = 1000,
                EndBlock = 2000,
                RoundInterval = 100,
                RequiredMedalCount = 10,
                TotalPrize = 1000,
                BattleTicketPolicy = new BattleTicketPolicy { Id = 1, Name = "Test" },
                RefreshTicketPolicy = new RefreshTicketPolicy { Id = 1, Name = "Test" },
                Rounds = new List<Round>()
            }
        };

        _serviceMock.Setup(x => x.GetSeasonsPagedAsync(1, 10, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync((seasons, 1, 1, false, false));

        var result = await _controller.GetSeasonsPaged(1, 10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedSeasonsResponse>(okResult.Value);
        
        Assert.Single(response.Seasons);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(1, response.TotalPages);
        Assert.False(response.HasNextPage);
        Assert.False(response.HasPreviousPage);
    }

    [Fact]
    public async Task GetSeasonsPaged_InvalidPageNumber_ReturnsBadRequest()
    {
        var result = await _controller.GetSeasonsPaged(0, 10);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page number must be greater than 0", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSeasonsPaged_InvalidPageSize_ReturnsBadRequest()
    {
        var result = await _controller.GetSeasonsPaged(1, 0);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page size must be between 1 and 100", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSeasonsPaged_PageSizeTooLarge_ReturnsBadRequest()
    {
        var result = await _controller.GetSeasonsPaged(1, 101);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page size must be between 1 and 100", badRequestResult.Value);
    }

    [Fact]
    public async Task GetSeasonsPaged_DefaultParameters_ReturnsCorrectPageInfo()
    {
        var seasons = new List<Season>();
        _serviceMock.Setup(x => x.GetSeasonsPagedAsync(1, 10, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync((seasons, 0, 0, false, false));

        var result = await _controller.GetSeasonsPaged();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedSeasonsResponse>(okResult.Value);
        
        Assert.Equal(1, response.PageNumber);
        Assert.Equal(10, response.PageSize);
    }
}

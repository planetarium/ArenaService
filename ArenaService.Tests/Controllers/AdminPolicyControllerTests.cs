using ArenaService.Controllers;
using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class AdminPolicyControllerTests
{
    private readonly AdminPolicyController _controller;
    private readonly Mock<IBattleTicketPolicyRepository> _battlePolicyRepoMock;
    private readonly Mock<IRefreshTicketPolicyRepository> _refreshPolicyRepoMock;

    public AdminPolicyControllerTests()
    {
        _battlePolicyRepoMock = new Mock<IBattleTicketPolicyRepository>();
        _refreshPolicyRepoMock = new Mock<IRefreshTicketPolicyRepository>();
        _controller = new AdminPolicyController(_battlePolicyRepoMock.Object, _refreshPolicyRepoMock.Object);
    }

    [Fact]
    public async Task GetBattleTicketPolicies_ReturnsOk()
    {
        var policies = new List<BattleTicketPolicy>
        {
            new BattleTicketPolicy
            {
                Id = 1,
                Name = "Default",
                DefaultTicketsPerRound = 5,
                MaxPurchasableTicketsPerRound = 10,
                MaxPurchasableTicketsPerSeason = 50,
                PurchasePrices = new List<decimal> { 1m, 2m, 3m }
            }
        };
        _battlePolicyRepoMock.Setup(x => x.GetAllBattlePoliciesAsync()).ReturnsAsync(policies);

        var result = await _controller.GetBattleTicketPolicies();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPolicies = Assert.IsType<List<BattleTicketPolicy>>(okResult.Value);
        Assert.Single(returnedPolicies);
    }

    [Fact]
    public async Task GetBattleTicketPolicy_Exists_ReturnsOk()
    {
        var policy = new BattleTicketPolicy { Id = 1, Name = "Test" };
        _battlePolicyRepoMock.Setup(x => x.GetBattlePolicyByIdAsync(1)).ReturnsAsync(policy);

        var result = await _controller.GetBattleTicketPolicy(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(policy, okResult.Value);
    }

    [Fact]
    public async Task GetBattleTicketPolicy_NotFound_ReturnsNotFound()
    {
        _battlePolicyRepoMock.Setup(x => x.GetBattlePolicyByIdAsync(999)).ReturnsAsync((BattleTicketPolicy?)null);

        var result = await _controller.GetBattleTicketPolicy(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateBattleTicketPolicy_ReturnsCreated()
    {
        var created = new BattleTicketPolicy
        {
            Id = 1,
            Name = "New Policy",
            DefaultTicketsPerRound = 5,
            MaxPurchasableTicketsPerRound = 10,
            MaxPurchasableTicketsPerSeason = 50,
            PurchasePrices = new List<decimal> { 1m, 2m }
        };
        _battlePolicyRepoMock.Setup(x => x.AddBattlePolicyAsync(It.IsAny<BattleTicketPolicy>()))
            .ReturnsAsync(created);

        var request = new CreateBattleTicketPolicyRequest
        {
            Name = "New Policy",
            DefaultTicketsPerRound = 5,
            MaxPurchasableTicketsPerRound = 10,
            MaxPurchasableTicketsPerSeason = 50,
            PurchasePrices = new List<decimal> { 1m, 2m }
        };

        var result = await _controller.CreateBattleTicketPolicy(request);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task GetRefreshTicketPolicies_ReturnsOk()
    {
        var policies = new List<RefreshTicketPolicy>
        {
            new RefreshTicketPolicy
            {
                Id = 1,
                Name = "Default",
                DefaultTicketsPerRound = 3,
                MaxPurchasableTicketsPerRound = 5,
                PurchasePrices = new List<decimal> { 1m }
            }
        };
        _refreshPolicyRepoMock.Setup(x => x.GetAllRefreshPoliciesAsync()).ReturnsAsync(policies);

        var result = await _controller.GetRefreshTicketPolicies();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPolicies = Assert.IsType<List<RefreshTicketPolicy>>(okResult.Value);
        Assert.Single(returnedPolicies);
    }

    [Fact]
    public async Task GetRefreshTicketPolicy_Exists_ReturnsOk()
    {
        var policy = new RefreshTicketPolicy { Id = 1, Name = "Test" };
        _refreshPolicyRepoMock.Setup(x => x.GetRefreshPolicyByIdAsync(1)).ReturnsAsync(policy);

        var result = await _controller.GetRefreshTicketPolicy(1);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(policy, okResult.Value);
    }

    [Fact]
    public async Task CreateRefreshTicketPolicy_ReturnsCreated()
    {
        var created = new RefreshTicketPolicy
        {
            Id = 1,
            Name = "New Refresh",
            DefaultTicketsPerRound = 3,
            MaxPurchasableTicketsPerRound = 5,
            PurchasePrices = new List<decimal> { 1m }
        };
        _refreshPolicyRepoMock.Setup(x => x.AddRefreshPolicyAsync(It.IsAny<RefreshTicketPolicy>()))
            .ReturnsAsync(created);

        var request = new CreateRefreshTicketPolicyRequest
        {
            Name = "New Refresh",
            DefaultTicketsPerRound = 3,
            MaxPurchasableTicketsPerRound = 5,
            PurchasePrices = new List<decimal> { 1m }
        };

        var result = await _controller.CreateRefreshTicketPolicy(request);

        Assert.IsType<CreatedAtActionResult>(result);
    }
}

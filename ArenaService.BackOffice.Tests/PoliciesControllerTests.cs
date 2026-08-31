using ArenaService.BackOffice.Controllers;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArenaService.BackOffice.Tests;

public class PoliciesControllerTests
{
    private readonly Mock<IBattleTicketPolicyRepository> _battleRepo = new();
    private readonly Mock<IRefreshTicketPolicyRepository> _refreshRepo = new();
    private readonly PoliciesController _controller;

    public PoliciesControllerTests()
    {
        _controller = new PoliciesController(
            _battleRepo.Object,
            _refreshRepo.Object,
            NullLogger<PoliciesController>.Instance
        );
    }

    private static List<decimal> Prices(int count) =>
        Enumerable.Range(1, count).Select(i => i * 0.1m).ToList();

    [Fact]
    public async Task GetBattlePolicies_ShouldProjectEveryPolicy()
    {
        _battleRepo
            .Setup(r => r.GetAllBattlePoliciesAsync())
            .ReturnsAsync(new List<BattleTicketPolicy>
            {
                new()
                {
                    Id = 1,
                    Name = "policy-a",
                    DefaultTicketsPerRound = 5,
                    MaxPurchasableTicketsPerRound = 4,
                    MaxPurchasableTicketsPerSeason = 3,
                    PurchasePrices = Prices(3)
                }
            });

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetBattlePolicies()).Result);
        var payload = Assert.IsType<ApiResponse<List<BattleTicketPolicyDto>>>(ok.Value);

        Assert.Single(payload.Data!);
        Assert.Equal("policy-a", payload.Data![0].Name);
        Assert.Equal(3, payload.Data![0].PurchasePrices.Count);
    }

    [Fact]
    public async Task AddBattlePolicy_WhenPriceCountDoesNotMatchSeasonMax_ShouldReturnBadRequest()
    {
        var result = await _controller.AddBattlePolicy(
            new AddBattleTicketPolicyRequest
            {
                Name = "bad",
                MaxPurchasableTicketsPerSeason = 5,
                PurchasePrices = Prices(3)
            }
        );

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _battleRepo.Verify(r => r.AddBattlePolicyAsync(It.IsAny<BattleTicketPolicy>()), Times.Never);
    }

    [Fact]
    public async Task AddBattlePolicy_WithMatchingPriceCount_ShouldPersist()
    {
        _battleRepo
            .Setup(r => r.AddBattlePolicyAsync(It.IsAny<BattleTicketPolicy>()))
            .ReturnsAsync((BattleTicketPolicy p) => p);

        var result = await _controller.AddBattlePolicy(
            new AddBattleTicketPolicyRequest
            {
                Name = "good",
                DefaultTicketsPerRound = 5,
                MaxPurchasableTicketsPerRound = 2,
                MaxPurchasableTicketsPerSeason = 3,
                PurchasePrices = Prices(3)
            }
        );

        Assert.IsType<OkObjectResult>(result.Result);
        _battleRepo.Verify(r => r.AddBattlePolicyAsync(It.Is<BattleTicketPolicy>(p => p.Name == "good")), Times.Once);
    }

    [Fact]
    public async Task AddRefreshPolicy_WithoutDefaultTicket_ShouldReturnBadRequest()
    {
        var result = await _controller.AddRefreshPolicy(
            new AddRefreshTicketPolicyRequest
            {
                Name = "bad",
                DefaultTicketsPerRound = 0,
                MaxPurchasableTicketsPerRound = 3,
                PurchasePrices = Prices(3)
            }
        );

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _refreshRepo.Verify(r => r.AddRefreshPolicyAsync(It.IsAny<RefreshTicketPolicy>()), Times.Never);
    }

    [Fact]
    public async Task AddRefreshPolicy_WhenPriceCountDoesNotMatchRoundMax_ShouldReturnBadRequest()
    {
        var result = await _controller.AddRefreshPolicy(
            new AddRefreshTicketPolicyRequest
            {
                Name = "bad",
                DefaultTicketsPerRound = 1,
                MaxPurchasableTicketsPerRound = 5,
                PurchasePrices = Prices(2)
            }
        );

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddRefreshPolicy_WithValidInput_ShouldPersist()
    {
        _refreshRepo
            .Setup(r => r.AddRefreshPolicyAsync(It.IsAny<RefreshTicketPolicy>()))
            .ReturnsAsync((RefreshTicketPolicy p) => p);

        var result = await _controller.AddRefreshPolicy(
            new AddRefreshTicketPolicyRequest
            {
                Name = "good",
                DefaultTicketsPerRound = 1,
                MaxPurchasableTicketsPerRound = 2,
                PurchasePrices = Prices(2)
            }
        );

        Assert.IsType<OkObjectResult>(result.Result);
        _refreshRepo.Verify(r => r.AddRefreshPolicyAsync(It.Is<RefreshTicketPolicy>(p => p.Name == "good")), Times.Once);
    }
}

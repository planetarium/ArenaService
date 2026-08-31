using ArenaService.BackOffice.Controllers;
using ArenaService.BackOffice.Models;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArenaService.BackOffice.Tests;

public class ConfirmTxControllerTests
{
    private readonly Mock<IBattleRepository> _battleRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly ConfirmTxController _controller;

    public ConfirmTxControllerTests()
    {
        _controller = new ConfirmTxController(
            _battleRepo.Object,
            _ticketRepo.Object,
            NullLogger<ConfirmTxController>.Instance
        );
    }

    [Fact]
    public async Task GetUnreviewedBattles_ShouldLowerCaseAvatarAddress()
    {
        var address = new PrivateKey().Address;
        _battleRepo
            .Setup(r => r.GetUnReviewedBattlesAsync())
            .ReturnsAsync(new List<Battle> { new() { Id = 7, AvatarAddress = address, SeasonId = 1, RoundId = 2, Token = "token" } });

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetUnreviewedBattles()).Result);
        var payload = Assert.IsType<ApiResponse<List<BattleDto>>>(ok.Value);

        Assert.Equal(address.ToString().ToLower(), payload.Data![0].AvatarAddress);
    }

    [Fact]
    public async Task GetUnreviewedTicketPurchases_ShouldMergeBothTicketTypes()
    {
        _ticketRepo
            .Setup(r => r.GetUnReviewedBattleTicketPurchasesAsync())
            .ReturnsAsync(new List<BattleTicketPurchaseLog>
            {
                new() { Id = 1, AvatarAddress = new PrivateKey().Address }
            });
        _ticketRepo
            .Setup(r => r.GetUnReviewedRefreshTicketPurchasesAsync())
            .ReturnsAsync(new List<RefreshTicketPurchaseLog>
            {
                new() { Id = 2, AvatarAddress = new PrivateKey().Address }
            });

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetUnreviewedTicketPurchases()).Result);
        var payload = Assert.IsType<ApiResponse<List<TicketPurchaseLogDto>>>(ok.Value);

        Assert.Equal(2, payload.Data!.Count);
        Assert.Contains(payload.Data!, d => d.TicketType == "battle");
        Assert.Contains(payload.Data!, d => d.TicketType == "refresh");
    }

    [Fact]
    public async Task MarkBattleAsReviewed_ShouldUpdateBattle()
    {
        _battleRepo
            .Setup(r => r.UpdateBattle(3, It.IsAny<Action<Battle>>()))
            .ReturnsAsync(new Battle { Id = 3, Token = "token" });

        Assert.IsType<OkObjectResult>((await _controller.MarkBattleAsReviewed(3)).Result);
        _battleRepo.Verify(r => r.UpdateBattle(3, It.IsAny<Action<Battle>>()), Times.Once);
    }

    [Theory]
    [InlineData("battle")]
    [InlineData("BATTLE")]
    public async Task MarkTicketPurchaseAsReviewed_WithBattleType_ShouldUpdateBattleLog(string type)
    {
        _ticketRepo
            .Setup(r => r.UpdateBattleTicketPurchaseLog(5, It.IsAny<Action<BattleTicketPurchaseLog>>()))
            .ReturnsAsync(new BattleTicketPurchaseLog { Id = 5 });

        Assert.IsType<OkObjectResult>((await _controller.MarkTicketPurchaseAsReviewed(5, type)).Result);
        _ticketRepo.Verify(
            r => r.UpdateBattleTicketPurchaseLog(5, It.IsAny<Action<BattleTicketPurchaseLog>>()),
            Times.Once
        );
        _ticketRepo.Verify(
            r => r.UpdateRefreshTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<RefreshTicketPurchaseLog>>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData("refresh")]
    [InlineData("Refresh")]
    public async Task MarkTicketPurchaseAsReviewed_WithRefreshType_ShouldUpdateRefreshLog(string type)
    {
        _ticketRepo
            .Setup(r => r.UpdateRefreshTicketPurchaseLog(6, It.IsAny<Action<RefreshTicketPurchaseLog>>()))
            .ReturnsAsync(new RefreshTicketPurchaseLog { Id = 6 });

        Assert.IsType<OkObjectResult>((await _controller.MarkTicketPurchaseAsReviewed(6, type)).Result);
        _ticketRepo.Verify(
            r => r.UpdateRefreshTicketPurchaseLog(6, It.IsAny<Action<RefreshTicketPurchaseLog>>()),
            Times.Once
        );
        _ticketRepo.Verify(
            r => r.UpdateBattleTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<BattleTicketPurchaseLog>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task MarkTicketPurchaseAsReviewed_WithUnknownType_ShouldReturnBadRequest()
    {
        var result = await _controller.MarkTicketPurchaseAsReviewed(7, "foo");

        Assert.IsType<BadRequestObjectResult>(result.Result);
        _ticketRepo.Verify(
            r => r.UpdateBattleTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<BattleTicketPurchaseLog>>()),
            Times.Never
        );
        _ticketRepo.Verify(
            r => r.UpdateRefreshTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<RefreshTicketPurchaseLog>>()),
            Times.Never
        );
    }
}

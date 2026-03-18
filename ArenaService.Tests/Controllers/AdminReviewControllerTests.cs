using ArenaService.Controllers;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.BattleTicket;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Models.RefreshTicket;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class AdminReviewControllerTests
{
    private readonly AdminReviewController _controller;
    private readonly Mock<IBattleRepository> _battleRepoMock;
    private readonly Mock<ITicketRepository> _ticketRepoMock;

    public AdminReviewControllerTests()
    {
        _battleRepoMock = new Mock<IBattleRepository>();
        _ticketRepoMock = new Mock<ITicketRepository>();
        _controller = new AdminReviewController(_battleRepoMock.Object, _ticketRepoMock.Object);
    }

    [Fact]
    public async Task GetUnreviewedBattles_ReturnsOk()
    {
        var battles = new List<Battle>
        {
            new Battle
            {
                Id = 1,
                AvatarAddress = new Address("0x0000000000000000000000000000000000000000"),
                SeasonId = 1,
                RoundId = 1,
                AvailableOpponentId = 1,
                Token = "test-token",
                BattleStatus = BattleStatus.TX_FAILED
            }
        };
        _battleRepoMock.Setup(x => x.GetUnReviewedBattlesAsync()).ReturnsAsync(battles);

        var result = await _controller.GetUnreviewedBattles();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBattles = Assert.IsType<List<Battle>>(okResult.Value);
        Assert.Single(returnedBattles);
    }

    [Fact]
    public async Task GetUnreviewedTicketPurchases_ReturnsOk()
    {
        _ticketRepoMock.Setup(x => x.GetUnReviewedBattleTicketPurchasesAsync())
            .ReturnsAsync(new List<BattleTicketPurchaseLog>());
        _ticketRepoMock.Setup(x => x.GetUnReviewedRefreshTicketPurchasesAsync())
            .ReturnsAsync(new List<RefreshTicketPurchaseLog>());

        var result = await _controller.GetUnreviewedTicketPurchases();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmBattle_Exists_ReturnsOk()
    {
        var battle = new Battle
        {
            Id = 1,
            AvatarAddress = new Address("0x0000000000000000000000000000000000000000"),
            SeasonId = 1,
            RoundId = 1,
            AvailableOpponentId = 1,
            Token = "test-token",
            Reviewed = true
        };
        _battleRepoMock.Setup(x => x.UpdateBattle(1, It.IsAny<Action<Battle>>()))
            .ReturnsAsync(battle);

        var result = await _controller.ConfirmBattle(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmBattle_NotFound_ReturnsNotFound()
    {
        _battleRepoMock.Setup(x => x.UpdateBattle(999, It.IsAny<Action<Battle>>()))
            .ThrowsAsync(new ArgumentException("Battle not found"));

        var result = await _controller.ConfirmBattle(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ConfirmTicketPurchase_BattleType_ReturnsOk()
    {
        var log = new BattleTicketPurchaseLog
        {
            Id = 1,
            AvatarAddress = new Address("0x0000000000000000000000000000000000000000"),
            SeasonId = 1,
            RoundId = 1,
            PurchaseStatus = PurchaseStatus.PENDING,
            PurchaseCount = 1,
            TxId = new TxId(new byte[32]),
            Reviewed = true
        };
        _ticketRepoMock.Setup(x => x.UpdateBattleTicketPurchaseLog(1, It.IsAny<Action<BattleTicketPurchaseLog>>()))
            .ReturnsAsync(log);

        var result = await _controller.ConfirmTicketPurchase(1, "battle");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmTicketPurchase_RefreshType_ReturnsOk()
    {
        var log = new RefreshTicketPurchaseLog
        {
            Id = 1,
            AvatarAddress = new Address("0x0000000000000000000000000000000000000000"),
            SeasonId = 1,
            RoundId = 1,
            PurchaseStatus = PurchaseStatus.PENDING,
            PurchaseCount = 1,
            TxId = new TxId(new byte[32]),
            Reviewed = true
        };
        _ticketRepoMock.Setup(x => x.UpdateRefreshTicketPurchaseLog(1, It.IsAny<Action<RefreshTicketPurchaseLog>>()))
            .ReturnsAsync(log);

        var result = await _controller.ConfirmTicketPurchase(1, "refresh");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmTicketPurchase_InvalidType_ReturnsBadRequest()
    {
        var result = await _controller.ConfirmTicketPurchase(1, "invalid");

        Assert.IsType<BadRequestObjectResult>(result);
        _ticketRepoMock.Verify(
            x => x.UpdateBattleTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<BattleTicketPurchaseLog>>()),
            Times.Never
        );
        _ticketRepoMock.Verify(
            x => x.UpdateRefreshTicketPurchaseLog(It.IsAny<int>(), It.IsAny<Action<RefreshTicketPurchaseLog>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ConfirmTicketPurchase_CaseInsensitive_ReturnsOk()
    {
        var log = new RefreshTicketPurchaseLog
        {
            Id = 1,
            AvatarAddress = new Address("0x0000000000000000000000000000000000000000"),
            SeasonId = 1,
            RoundId = 1,
            PurchaseStatus = PurchaseStatus.PENDING,
            PurchaseCount = 1,
            TxId = new TxId(new byte[32]),
            Reviewed = true
        };
        _ticketRepoMock.Setup(x => x.UpdateRefreshTicketPurchaseLog(1, It.IsAny<Action<RefreshTicketPurchaseLog>>()))
            .ReturnsAsync(log);

        var result = await _controller.ConfirmTicketPurchase(1, "Refresh");

        Assert.IsType<OkObjectResult>(result);
    }
}

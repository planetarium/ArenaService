using ArenaService.Controllers;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class AdminLeaderboardControllerTests
{
    private readonly AdminLeaderboardController _controller;
    private readonly Mock<ILeaderboardRepository> _leaderboardRepoMock;

    public AdminLeaderboardControllerTests()
    {
        _leaderboardRepoMock = new Mock<ILeaderboardRepository>();
        _controller = new AdminLeaderboardController(_leaderboardRepoMock.Object);
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsOk()
    {
        var address = new Address("0x0000000000000000000000000000000000000000");
        var participant = new Participant
        {
            AvatarAddress = address,
            SeasonId = 1,
            Score = 1200,
            TotalWin = 10,
            TotalLose = 5,
            User = new User
            {
                AvatarAddress = address,
                AgentAddress = address,
                NameWithHash = "TestUser#1234",
                Level = 100,
                PortraitId = 1,
                Cp = 5000L
            }
        };

        var leaderboard = new List<(Participant Participant, int Score, int Rank)>
        {
            (participant, 1200, 1)
        };
        _leaderboardRepoMock.Setup(x => x.GetLeaderboardAsync(1)).ReturnsAsync(leaderboard);

        var result = await _controller.GetLeaderboard(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DownloadLeaderboardCsv_ReturnsFile()
    {
        var csvBytes = "avatar_address,agent_address\ntest,test"u8.ToArray();
        _leaderboardRepoMock.Setup(x => x.GenerateLeaderboardCsvAsync(1)).ReturnsAsync(csvBytes);

        var result = await _controller.DownloadLeaderboardCsv(1);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.Equal("leaderboard_season_1.csv", fileResult.FileDownloadName);
        Assert.Equal(csvBytes, fileResult.FileContents);
    }
}

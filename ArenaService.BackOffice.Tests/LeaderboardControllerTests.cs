using ArenaService.BackOffice.Controllers;
using ArenaService.BackOffice.Models;
using ArenaService.Client;
using ArenaService.Options;
using ArenaService.Shared.Models;
using ArenaService.Shared.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArenaService.BackOffice.Tests;

public class LeaderboardControllerTests
{
    private readonly Mock<ILeaderboardRepository> _leaderboardRepo = new();
    private readonly Mock<ISeasonRepository> _seasonRepo = new();
    private readonly Mock<IHeadlessClient> _headlessClient = new();
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _controller = new LeaderboardController(
            _leaderboardRepo.Object,
            _seasonRepo.Object,
            _headlessClient.Object,
            Microsoft.Extensions.Options.Options.Create(
                new HeadlessOptions { Planet = "odin", HeadlessEndpoint = new Uri("http://localhost") }
            ),
            NullLogger<LeaderboardController>.Instance
        );
    }

    private static (Participant, int, int) CreateEntry(Address avatarAddress, Address agentAddress, int score, int rank) =>
        (
            new Participant
            {
                AvatarAddress = avatarAddress,
                SeasonId = 1,
                Score = score,
                TotalWin = 3,
                TotalLose = 2,
                User = new User
                {
                    AvatarAddress = avatarAddress,
                    AgentAddress = agentAddress,
                    Level = 200,
                    Cp = 12345,
                    PortraitId = 10,
                    NameWithHash = "tester#1234"
                }
            },
            score,
            rank
        );

    [Fact]
    public async Task GetLeaderboard_ShouldLowerCaseAddresses()
    {
        var avatarAddress = new PrivateKey().Address;
        var agentAddress = new PrivateKey().Address;
        _leaderboardRepo
            .Setup(r => r.GetLeaderboardAsync(1))
            .ReturnsAsync(new List<(Participant, int, int)> { CreateEntry(avatarAddress, agentAddress, 1500, 1) });

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetLeaderboard(1)).Result);
        var payload = Assert.IsType<ApiResponse<List<LeaderboardEntryDto>>>(ok.Value);

        Assert.Equal(avatarAddress.ToString().ToLower(), payload.Data![0].AvatarAddress);
        Assert.Equal(agentAddress.ToString().ToLower(), payload.Data![0].AgentAddress);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldProjectRankAndRecord()
    {
        _leaderboardRepo
            .Setup(r => r.GetLeaderboardAsync(1))
            .ReturnsAsync(new List<(Participant, int, int)>
            {
                CreateEntry(new PrivateKey().Address, new PrivateKey().Address, 1500, 1),
                CreateEntry(new PrivateKey().Address, new PrivateKey().Address, 1400, 2)
            });

        var ok = Assert.IsType<OkObjectResult>((await _controller.GetLeaderboard(1)).Result);
        var payload = Assert.IsType<ApiResponse<List<LeaderboardEntryDto>>>(ok.Value);

        Assert.Equal(new[] { 1, 2 }, payload.Data!.Select(e => e.Rank));
        Assert.Equal(new[] { 1500, 1400 }, payload.Data!.Select(e => e.Score));
        Assert.Equal(3, payload.Data![0].TotalWin);
        Assert.Equal(2, payload.Data![0].TotalLose);
    }

    [Fact]
    public async Task GetLeaderboardCsv_ShouldNameFileWithPlanetAndBlockRange()
    {
        _seasonRepo
            .Setup(r => r.GetSeasonAsync(1, It.IsAny<Func<IQueryable<Season>, IQueryable<Season>>>()))
            .ReturnsAsync(new Season { Id = 1, SeasonGroupId = 7, StartBlock = 100, EndBlock = 200 });
        _leaderboardRepo.Setup(r => r.GenerateLeaderboardCsvAsync(1)).ReturnsAsync(new byte[] { 1, 2, 3 });

        var file = Assert.IsType<FileContentResult>(await _controller.GetLeaderboardCsv(1));

        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("odin_leaderboard_group_7_100_200.csv", file.FileDownloadName);
        Assert.Equal(3, file.FileContents.Length);
    }
}

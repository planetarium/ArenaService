using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace ArenaService.Tests.Controllers;

public class LeaderboardControllerTests
{
    private readonly LeaderboardController _controller;
    private Mock<IParticipantRepository> _participantRepoMock;
    private Mock<ILeaderboardRepository> _leaderboardRepoMock;

    public LeaderboardControllerTests()
    {
        var participantRepoMock = new Mock<IParticipantRepository>();
        _participantRepoMock = participantRepoMock;
        var leaderboardRepoMock = new Mock<ILeaderboardRepository>();
        _leaderboardRepoMock = leaderboardRepoMock;
        _controller = new LeaderboardController(
            _leaderboardRepoMock.Object,
            _participantRepoMock.Object
        );
    }

    [Fact]
    public async Task Join_ActivatedSeasonsExist_ReturnsOk()
    {
        _participantRepoMock
            .Setup(repo => repo.GetParticipantByAvatarAddressAsync(1, "test"))
            .ReturnsAsync(
                new Participant
                {
                    Id = 1,
                    AvatarAddress = "test",
                    NameWithHash = "test",
                    PortraitId = 1
                }
            );
        _leaderboardRepoMock
            .Setup(repo => repo.GetMyRankAsync(1, 1))
            .ReturnsAsync(
                new LeaderboardEntry
                {
                    Id = 1,
                    ParticipantId = 1,
                    Participant = new Participant
                    {
                        AvatarAddress = "test",
                        NameWithHash = "test",
                        PortraitId = 1
                    },
                    Rank = 1,
                    SeasonId = 1,
                    TotalScore = 1000
                }
            );

        var result = await _controller.GetMyRank(1, "test");

        var okResult = Assert.IsType<Ok<LeaderboardEntryResponse>>(result.Result);
    }
}

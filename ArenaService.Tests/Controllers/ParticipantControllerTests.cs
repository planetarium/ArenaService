using ArenaService.Controllers;
using ArenaService.Dtos;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ArenaService.Tests.Controllers;

public class ParticipantControllerTests
{
    private readonly ParticipantController _controller;
    private Mock<ISeasonRepository> _seasonRepositoryMock;
    private Mock<IParticipantRepository> _participantRepositoryMock;
    private SeasonService _seasonService;
    private ParticipantService _participantService;

    public ParticipantControllerTests()
    {
        var seasonRepositoryMock = new Mock<ISeasonRepository>();
        var participantRepositoryMock = new Mock<IParticipantRepository>();
        _seasonRepositoryMock = seasonRepositoryMock;
        _participantRepositoryMock = participantRepositoryMock;
        _seasonService = new SeasonService(_seasonRepositoryMock.Object);
        _participantService = new ParticipantService(_participantRepositoryMock.Object);
        _controller = new ParticipantController(_participantService);
    }

    [Fact]
    public async Task Join_ActivatedSeasonsExist_ReturnsOk()
    {
        var season = new Season
        {
            Id = 1,
            StartBlockIndex = 700,
            EndBlockIndex = 899,
            TicketRefillInterval = 600,
            IsActivated = true,
            Participants = new List<Participant>(),
            BattleLogs = new List<BattleLog>(),
            Leaderboard = new List<LeaderboardEntry>()
        };
        var participant = new Participant
        {
            Id = 1,
            AvatarAddress = "test",
            NameWithHash = "test",
            SeasonId = 1,
            PortraitId = 1,
            Season = season,
            BattleLogs = new List<BattleLog>(),
            Leaderboard = new List<LeaderboardEntry>()
        };

        _seasonRepositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync([season]);
        _participantRepositoryMock
            .Setup(repo =>
                repo.InsertParticipantToSpecificSeason(
                    season.Id,
                    participant.AvatarAddress,
                    participant.NameWithHash,
                    participant.PortraitId
                )
            )
            .ReturnsAsync(participant);

        var result = await _controller.Join(
            1,
            new JoinRequest { AvatarAddress = "test", AuthToken = "test" }
        );

        var okResult = Assert.IsType<OkObjectResult>(result);

        Assert.True(okResult.StatusCode == 201);
    }
}

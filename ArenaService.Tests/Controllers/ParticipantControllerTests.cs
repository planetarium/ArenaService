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
        _controller = new ParticipantController(_participantService, _seasonService);
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
            IsActivated = true
        };
        var participant = new Participant
        {
            Id = 1,
            AvatarAddress = "test",
            NameWithHash = "test",
            SeasonId = 1,
            PortraitId = 1,
            Season = season
        };

        _seasonRepositoryMock.Setup(repo => repo.GetSeasonAsync(season.Id)).ReturnsAsync(season);
        _participantRepositoryMock
            .Setup(repo =>
                repo.InsertParticipantToSpecificSeasonAsync(
                    season.Id,
                    participant.AvatarAddress,
                    participant.NameWithHash,
                    participant.PortraitId
                )
            )
            .ReturnsAsync(participant);

        var result = await _controller.Join(
            1,
            new JoinRequest
            {
                AvatarAddress = "test",
                AuthToken = "test",
                NameWithHash = "test",
                PortraitId = 1
            }
        );

        var okResult = Assert.IsType<Created>(result.Result);
    }

    [Fact]
    public async Task Join_ActivatedSeasonsNotExist_ReturnsNotFound()
    {
        var season = new Season
        {
            Id = 1,
            StartBlockIndex = 700,
            EndBlockIndex = 899,
            TicketRefillInterval = 600,
            IsActivated = false
        };
        var participant = new Participant
        {
            Id = 1,
            AvatarAddress = "test",
            NameWithHash = "test",
            SeasonId = 1,
            PortraitId = 1,
            Season = season
        };

        _seasonRepositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync([season]);
        _participantRepositoryMock
            .Setup(repo =>
                repo.InsertParticipantToSpecificSeasonAsync(
                    season.Id,
                    participant.AvatarAddress,
                    participant.NameWithHash,
                    participant.PortraitId
                )
            )
            .ReturnsAsync(participant);

        var result = await _controller.Join(
            1,
            new JoinRequest
            {
                AvatarAddress = "test",
                AuthToken = "test",
                NameWithHash = "test",
                PortraitId = 1
            }
        );

        Assert.IsType<NotFound<string>>(result.Result);
    }

    [Fact]
    public async Task Join_SeasonsNotExist_ReturnsNotFound()
    {
        var season = new Season
        {
            Id = 1,
            StartBlockIndex = 700,
            EndBlockIndex = 899,
            TicketRefillInterval = 600,
            IsActivated = true
        };
        var participant = new Participant
        {
            Id = 1,
            AvatarAddress = "test",
            NameWithHash = "test",
            SeasonId = 1,
            PortraitId = 1,
            Season = season
        };

        _seasonRepositoryMock.Setup(repo => repo.GetActivatedSeasonsAsync()).ReturnsAsync([season]);
        _participantRepositoryMock
            .Setup(repo =>
                repo.InsertParticipantToSpecificSeasonAsync(
                    season.Id,
                    participant.AvatarAddress,
                    participant.NameWithHash,
                    participant.PortraitId
                )
            )
            .ReturnsAsync(participant);

        var result = await _controller.Join(
            2,
            new JoinRequest
            {
                AvatarAddress = "test",
                AuthToken = "test",
                NameWithHash = "test",
                PortraitId = 1
            }
        );

        var okResult = Assert.IsType<NotFound<string>>(result.Result);
    }
}
